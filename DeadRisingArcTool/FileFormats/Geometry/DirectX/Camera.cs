﻿using DeadRisingArcTool.FileFormats.Geometry.DirectX.Misc;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeadRisingArcTool.FileFormats.Geometry.DirectX
{
    public class Camera : IRenderable
    {
        public float Speed { get; set; } = 1.0f;
        public float SpeedModifier { get; set; } = 0.2f;

        // Constant directional vectors:
        private static readonly Vector3 DefaultUp = new Vector3(0.0f, 1.0f, 0.0f);
        private static readonly Vector3 DefaultDown = new Vector3(0.0f, -1.0f, 0.0f);
        private static readonly Vector3 DefaultForward = new Vector3(0.0f, 0.0f, -1.0f);
        private static readonly Vector3 DefaultBackward = new Vector3(0.0f, 0.0f, 1.0f);
        private static readonly Vector3 DefaultRight = new Vector3(1.0f, 0.0f, 0.0f);
        private static readonly Vector3 DefaultLeft = new Vector3(-1.0f, 0.0f, 0.0f);

        // Directional vectors based on the camera's current position and rotation.
        public Vector3 CamForward { get; private set; } = DefaultForward;
        public Vector3 CamBackward { get; private set; } = DefaultBackward;
        public Vector3 CamRight { get; private set; } = DefaultRight;
        public Vector3 CamLeft { get; private set; } = DefaultLeft;

        private Vector3 position;
        public Vector3 Position { get { return this.position; } set { this.position = value; ComputePosition(); } }
        private Vector2 rotation;
        public Vector2 Rotation { get { return this.rotation; } set { this.rotation = value; ComputePosition(); } }
        private Vector3 lookAt;
        public Vector3 LookAt { get { return this.lookAt; } set { this.lookAt = value; ComputePosition(); } }
        private Vector3 upVector;
        public Vector3 UpVector { get { return this.upVector; } set { this.upVector = value; ComputePosition(); } }

        public Matrix ViewMatrix
        {
            get
            {
                return Matrix.LookAtRH(this.Position, this.LookAt, this.UpVector);
            }
        }

        public Camera()
        {
            // Setup Camera vectors with default values.
            this.position = new Vector3(0.0f, 0.0f, 5.0f);
            this.rotation = new Vector2(0.0f, 0.0f);
            this.lookAt = new Vector3(0.0f, 0.0f, 0.0f);
            this.upVector = new Vector3(0.0f, 1.0f, 0.0f);

            ComputePosition();
        }

        private void ComputePosition()
        {
            // Update the direction we are looking in.
            Matrix camRotation = Matrix.RotationYawPitchRoll(this.rotation.X, this.rotation.Y, 0.0f);
            this.lookAt = Vector3.TransformCoordinate(DefaultForward, camRotation) + this.position;

            // Calculate up direction based on the current rotation.
            this.upVector = Vector3.TransformCoordinate(DefaultUp, camRotation);

            // Update directional vectors based on our new rotation.
            camRotation = Matrix.RotationYawPitchRoll(this.rotation.X, this.rotation.Y, 0.0f);
            this.CamForward = Vector3.TransformCoordinate(DefaultForward, camRotation);
            this.CamBackward = Vector3.TransformCoordinate(DefaultBackward, camRotation);
            this.CamRight = Vector3.TransformCoordinate(DefaultRight, camRotation);
            this.CamLeft = Vector3.TransformCoordinate(DefaultLeft, camRotation);
        }

        #region IRenderable

        public bool InitializeGraphics(RenderManager manager)
        {
            return true;
        }

        public bool DrawFrame(RenderManager manager)
        {
            // Update camera position.
            if (manager.InputManager.ButtonPressed(InputAction.MoveForward) == true || manager.InputManager.ButtonHeld(InputAction.MoveForward) == true)
                this.position += this.CamForward * this.Speed;
            if (manager.InputManager.ButtonPressed(InputAction.MoveBackward) == true || manager.InputManager.ButtonHeld(InputAction.MoveBackward) == true)
                this.position += this.CamBackward * this.Speed;
            if (manager.InputManager.ButtonPressed(InputAction.StrafeLeft) == true || manager.InputManager.ButtonHeld(InputAction.StrafeLeft) == true)
                this.position += this.CamLeft * this.Speed;
            if (manager.InputManager.ButtonPressed(InputAction.StrafeRight) == true || manager.InputManager.ButtonHeld(InputAction.StrafeRight) == true)
                this.position += this.CamRight * this.Speed;
            if (manager.InputManager.ButtonPressed(InputAction.MoveUp) == true || manager.InputManager.ButtonHeld(InputAction.MoveUp) == true)
                this.position += DefaultUp * this.Speed;
            if (manager.InputManager.ButtonPressed(InputAction.MoveDown) == true || manager.InputManager.ButtonHeld(InputAction.MoveDown) == true)
                this.position += DefaultDown * this.Speed;

            // Check for controller camera movement.
            if (manager.InputManager.GamepadThumbSticks[0] > 0)
                this.position += this.CamRight * this.Speed * ((float)manager.InputManager.GamepadThumbSticks[0] / (float)short.MaxValue);
            if (manager.InputManager.GamepadThumbSticks[0] < 0)
                this.position += this.CamLeft * this.Speed * -((float)manager.InputManager.GamepadThumbSticks[0] / (float)short.MaxValue);
            if (manager.InputManager.GamepadThumbSticks[1] > 0)
                this.position += this.CamForward * this.Speed * ((float)manager.InputManager.GamepadThumbSticks[1] / (float)short.MaxValue);
            if (manager.InputManager.GamepadThumbSticks[1] < 0)
                this.position += this.CamBackward * this.Speed * -((float)manager.InputManager.GamepadThumbSticks[1] / (float)short.MaxValue);

            // Update camera speed.
            if (manager.InputManager.ButtonPressed(InputAction.CamSpeedIncrease) == true ||
                manager.InputManager.ButtonHeld(InputAction.CamSpeedIncrease) == true || manager.InputManager.GamepadTriggers[1] > 0)
            {
                Speed += SpeedModifier;
                if (Speed <= 0) { Speed = SpeedModifier; }
            }
            if (manager.InputManager.ButtonPressed(InputAction.CamSpeedDecrease) == true ||
                manager.InputManager.ButtonHeld(InputAction.CamSpeedDecrease) == true || manager.InputManager.GamepadTriggers[0] > 0)
            {
                Speed -= SpeedModifier;
                if (Speed <= 0) { Speed = SpeedModifier; }
            }

            // Check for mouse movement.
            if (manager.InputManager.ButtonPressed(InputAction.LeftClick) == true || manager.InputManager.ButtonHeld(InputAction.LeftClick) == true)
            {
                // Update camera rotation.
                this.rotation.X -= manager.InputManager.MousePositionDelta[0] * 0.005f;
                this.rotation.Y -= manager.InputManager.MousePositionDelta[1] * 0.005f;
            }

            // Check for controller camera rotation.
            if (manager.InputManager.GamepadThumbSticks[2] != 0 || manager.InputManager.GamepadThumbSticks[3] != 0)
            {
                // Update the camera position.
                this.rotation.X += -((float)manager.InputManager.GamepadThumbSticks[2] / (float)short.MaxValue) * 0.075f;
                this.rotation.Y += -((float)manager.InputManager.GamepadThumbSticks[3] / (float)short.MaxValue) * 0.075f;
            }

            // Update camera vectors.
            ComputePosition();

            return true;
        }

        public void DrawObjectPropertiesUI(RenderManager manager)
        {

        }

        public void CleanupGraphics(RenderManager manager)
        {
        }

        public bool DoClippingTest(RenderManager manager, FastBoundingBox viewBox)
        {
            return false;
        }

        #endregion
    }
}

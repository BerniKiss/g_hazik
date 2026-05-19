using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {
        // kamera pozi
        private Vector3D<float> _position = new Vector3D<float>(0f, 0f, 8f);

        // forward, right, up vektorok
        private Vector3D<float> _forward = new Vector3D<float>(0f, 0f, -1f);
        private Vector3D<float> _right = new Vector3D<float>(1f, 0f, 0f);
        private Vector3D<float> _up = new Vector3D<float>(0f, 1f, 0f);

        private const float MoveStep = 0.3f;
        private const float TurnStep = (float)(Math.PI / 180 * 5);

        // pozicio
        public Vector3D<float> Position => _position;

        // a kamera elott levo pont
        public Vector3D<float> Target => _position + _forward;

        // fel vektor
        public Vector3D<float> UpVector => _up;

        // elore mozgas (ny fel)
        public void MoveForward()
        {
            _position += _forward * MoveStep;
        }

        // hatra mozgas (le)
        public void MoveBackward()
        {
            _position -= _forward * MoveStep;
        }

        // balra (ny bal)
        public void StrafeLeft()
        {
            _position -= _right * MoveStep;
        }

        // jobbra  (ny jobb)
        public void StrafeRight()
        {
            _position += _right * MoveStep;
        }

        // felfele mozgas (Q)
        public void MoveUp()
        {
            _position += _up * MoveStep;
        }

        // lefele mozgas (E)
        public void MoveDown()
        {
            _position -= _up * MoveStep;
        }

        // balra fordulas Y tengely korul (A)
        public void TurnLeft()
        {
            RotateAroundAxis(new Vector3D<float>(0f, 1f, 0f), TurnStep);
        }

        // jobbra fordulas Y tengely korul (D)
        public void TurnRight()
        {
            RotateAroundAxis(new Vector3D<float>(0f, 1f, 0f), -TurnStep);
        }

        // felfelé bil sajat jobb tengely (W)
        public void TiltUp()
        {
            RotateAroundAxis(_right, TurnStep);
        }

        // lefele billes sajat right tengely (S) 
        public void TiltDown()
        {
            RotateAroundAxis(_right, -TurnStep);
        }

        // tetszoleges tengely
        private void RotateAroundAxis(Vector3D<float> axis, float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            _forward = Vector3D.Normalize(
                _forward * cos +
                Vector3D.Cross(axis, _forward) * sin +
                axis * Vector3D.Dot(axis, _forward) * (1 - cos));

            _right = Vector3D.Normalize(
                _right * cos +
                Vector3D.Cross(axis, _right) * sin +
                axis * Vector3D.Dot(axis, _right) * (1 - cos));

            _up = Vector3D.Normalize(Vector3D.Cross(_right, _forward) * -1f);
        }
    }
}
using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    internal class CubeArrangementModel
    {
        /// <summary>
        /// Gets or sets wheather the animation should run or it should be frozen.
        /// </summary>
        public bool AnimationEnabeld { get; set; } = false;

        /// <summary>
        /// The time of the simulation. It helps to calculate time dependent values.
        /// </summary>
        private double Time { get; set; } = 0;


        // a felso lap yi==2  0tol PI/2ig 
        public double TopSliceAngle { get; private set; } = 0;

        // irany +1 = space irany, -1 = backspace 
        private int _sliceDirection = 0;

        // folyamatban van-e a forgatas
        private bool _rotating = false;

        // a forgatas clejanak szoge
        private double _targetAngle = 0;

        // forgatas ssebessege
        private const double RotationSpeed = Math.PI / 2 / 0.5; // 90 f 0.5 mp alatt

        /// <summary>
        /// The value by which the center cube is scaled. It varies between 0.8 and 1.2 with respect to the original size.
        /// </summary>
        // public double CenterCubeScale { get; private set; } = 1;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        // public double DiamondCubeAngleOwnRevolution { get; private set; } = 0;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        // public double DiamondCubeAngleRevolutionOnGlobalY { get; private set; } = 0;

        // space lenyomva 90 fokos forgat
        public void StartRotationPositive()
        {
            if (_rotating) return;
            _sliceDirection = 1;
            _targetAngle = TopSliceAngle + Math.PI / 2;
            _rotating = true;
        }

        // backspace y -90 fokos fprgat
        public void StartRotationNegative()
        {
            if (_rotating) return;
            _sliceDirection = -1;
            _targetAngle = TopSliceAngle - Math.PI / 2;
            _rotating = true;
        }

        internal void AdvanceTime(double deltaTime)
        {
            if (!_rotating)
                return;

            double step = RotationSpeed * deltaTime * _sliceDirection;
            TopSliceAngle += step;
            Console.WriteLine("SZOG: " + TopSliceAngle);

            if (_sliceDirection == 1 && TopSliceAngle >= _targetAngle)
            {
                TopSliceAngle = _targetAngle;
                _rotating = false;
            }
            else if (_sliceDirection == -1 && TopSliceAngle <= _targetAngle)
            {
                TopSliceAngle = _targetAngle;
                _rotating = false;
            }
        }

        // visszaadja a felso lap forgatasat mint Matrix4X4
        public Matrix4X4<float> TopSliceRotationMatrix =>
            Matrix4X4.CreateRotationY((float)TopSliceAngle);

        // megmondja hogy egy (xi,yi,zi) indexu kocka a felso lapon van-e
        public bool IsOnTopSlice(int xi, int yi, int zi) => yi == 2;
    }
}

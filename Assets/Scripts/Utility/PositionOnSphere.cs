using System;
using UnityEngine;

namespace VRVis.Utilities {
    public class PositionOnSphere {


        // positionOnSphere is the X,Y,Z point on the surface of the sphere (in world space).
        public static void PositionObjectOnSphere(Transform sphere, Transform objectOnSphere, Vector3 positionOnSphere) {

            // Get our X,Y,Z on the sphere as if the sphere were at the origin;
            Vector3 originPositionOnSphere = objectOnSphere.position - sphere.position;

            // Theta = rotation about the y-Axis. it can be from 0->360
            float theta = 0f;
            // Phi = rotation about the z-Axis. It can be from 0 to 180.
            // angle going from the north pole to the south pole
            float phi = 0f;

            // these 2 angle (plus a radius) uniquely define a point on the sphere.
            // So we will take our X,Y,Z point and convert it to these co-ordinates
            // this will get the proper rotation for our object to sit on the sphere correctly.
            GetSphericalCoordinates(originPositionOnSphere, ref theta, ref phi);


            Vector3 rotateAngles = new Vector3(0f, theta, phi);
            // If our object is NOT a child of the sphere and the sphere might be rotated
            // you need to add in the sphere's rotation, since those x,y,z might not be
            // in the same spot on a rotated sphere.
            rotateAngles += sphere.transform.eulerAngles; // only if your not the child
            objectOnSphere.Rotate(rotateAngles);

            objectOnSphere.transform.position = positionOnSphere;
        }

        public static void GetSphericalCoordinates(Vector3 originPositionOnSphere, ref float theta, ref float phi) {

            // all the following formulas are basic Math Spherical Coordinate formulas
            float x = originPositionOnSphere.x;
            float y = originPositionOnSphere.y;
            float z = originPositionOnSphere.z;
            float radius = Mathf.Sqrt(x * x + y * y + z * z);
            if (radius < 0)
                throw new ArgumentException("Radius Less than 0 in GetSphericalCoordinate");

            // ACos will always be between 0 and 180 so we are good
            // we need to clamp these just in case rounding errors
            // make them 1.00001  or -1.0005
            float phiValue = y / radius;
            phiValue = Mathf.Clamp(phiValue, -1f, 1f);
            phi = Mathf.Acos(phiValue);

            float thetaValue = z / (radius * Mathf.Sin(phi));
            thetaValue = Mathf.Clamp(thetaValue, -1f, 1f);
            theta = Mathf.Asin(thetaValue);



            // Both these angle are in radians
            // Lets make them degrees
            phi = phi * 180f / Mathf.PI;
            theta = theta * 180f / Mathf.PI;

            // Asin returns a number between -90 and 90
            // Since this can be from 0->360 we need have two options each for positive and negative angles
            if (theta < 0) {
                // Quadrant III
                if (x < 0) {
                    theta = 180f - theta;
                }
                //Quadrant IV
                else {
                    theta = 360 + theta;
                }
            }
            else {
                // Quadrant II
                if (x < 0) {
                    theta += 90;
                }
                // Quadrant I we do nothing
            }

            // Now theta is 0->360 *CounterClockwise* from the *X-Axis* (Normal Math)
            // we need it *ClockWise* from the *Z-Axis* (Unity)
            theta -= 90;
            theta = 360 - theta;
            if (theta >= 360)
                theta -= 360;
        }
              
        public static void CartesianToSpherical(Vector3 cartCoords, out float outRadius, out float outPolar, out float outElevation) {
            if (cartCoords.x == 0)
                cartCoords.x = Mathf.Epsilon;
            outRadius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                            + (cartCoords.y * cartCoords.y)
                            + (cartCoords.z * cartCoords.z));
            outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);
            if (cartCoords.x < 0)
                outPolar += Mathf.PI;
            outElevation = Mathf.Asin(cartCoords.y / outRadius);
        }

        public static SphericalCoordinates CartesianToSpherical(Vector3 cartCoords) {
            if (cartCoords.x == 0)
                cartCoords.x = Mathf.Epsilon;

            float radius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                            + (cartCoords.y * cartCoords.y)
                            + (cartCoords.z * cartCoords.z));
            float polar = Mathf.Atan(cartCoords.z / cartCoords.x);
            if (cartCoords.x < 0)
                polar += Mathf.PI;
            float elevation = Mathf.Asin(cartCoords.y / radius);                      

            return new SphericalCoordinates(radius, polar, elevation);
        }

        public static SphericalCoordinates CartesianToSpherical(Vector3 cartCoords, Vector3 sphereOrigin) {
            cartCoords = cartCoords - sphereOrigin;

            if (cartCoords.x == 0)
                cartCoords.x = Mathf.Epsilon;

            float radius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                            + (cartCoords.y * cartCoords.y)
                            + (cartCoords.z * cartCoords.z));
            float polar = Mathf.Atan(cartCoords.z / cartCoords.x);

            if (cartCoords.x < 0)
                polar += Mathf.PI;
            float elevation = Mathf.Asin(cartCoords.y / radius);

            return new SphericalCoordinates(radius, polar, elevation);
        }

        public static void SphericalToCartesian(float radius, float polar, float elevation, out Vector3 outCart) {
            float a = radius * Mathf.Cos(elevation);
            outCart.x = a * Mathf.Cos(polar);
            outCart.y = radius * Mathf.Sin(elevation);
            outCart.z = a * Mathf.Sin(polar);
        }

        public static Vector3 SphericalToCartesian(float radius, float polar, float elevation) {
            float a = radius * Mathf.Cos(elevation);
            Vector3 outCart = new Vector3();
            outCart.x = a * Mathf.Cos(polar);
            outCart.y = radius * Mathf.Sin(elevation);
            outCart.z = a * Mathf.Sin(polar);

            return outCart;
        }

        public static Vector3 SphericalToCartesian(float radius, float polar, float elevation, Vector3 sphereOrigin) {
            float a = radius * Mathf.Cos(elevation);
            Vector3 outCart = new Vector3();

            outCart.x = a * Mathf.Cos(polar);
            outCart.y = radius * Mathf.Sin(elevation);
            outCart.z = a * Mathf.Sin(polar);

            outCart = outCart + sphereOrigin;

            return outCart;
        }

        public static Vector3 SphericalToCartesian(SphericalCoordinates sphericalCoordinates) {
            float a = sphericalCoordinates.radius * Mathf.Cos(sphericalCoordinates.elevation);
            Vector3 outCart = new Vector3();
            outCart.x = a * Mathf.Cos(sphericalCoordinates.polar);
            outCart.y = sphericalCoordinates.radius * Mathf.Sin(sphericalCoordinates.elevation);
            outCart.z = a * Mathf.Sin(sphericalCoordinates.polar);

            return outCart;
        }

        public static double SphereIntersect(float sphereRadius, Vector3 sphereOrigin, Vector3 rayOrigin, Vector3 rayDirection) {
            Vector3 L = rayOrigin - sphereOrigin;
            float a = Vector3.Dot(rayDirection, rayDirection);
            float b = 2 * Vector3.Dot(rayDirection, L);
            float c = Vector3.Dot(L, L) - sphereRadius * sphereRadius;

            double t0 = -1.0;
            double t1 = -1.0;

            if (!SolveQuadratic(a, b, c, ref t0, ref t1)) {
                return -1.0;
            }

            if (t0 > t1) {
                double tmp = t1;
                t1 = t0;
                t0 = tmp;
            }

            if (t0 < Mathf.Epsilon) {
                t0 = t1;
                if (t0 < Mathf.Epsilon) {
                    return -1.0;
                }
            }

            return t0;
        }

        private static bool SolveQuadratic(double a, double b, double c, ref double x0, ref double x1) { 
            double discr = b * b - 4 * a * c; 
            if (discr < 0) return false; 
            else if (discr == 0) x0 = x1 = -0.5f * b / a; 
            else {
                double q = (b > Mathf.Epsilon) ?
                    -0.5 * (b + Math.Sqrt(discr)) :
                    -0.5 * (b - Math.Sqrt(discr));
                x0 = q / a; 
                x1 = c / q; 
            }
 
            return true; 
        } 
    }

}
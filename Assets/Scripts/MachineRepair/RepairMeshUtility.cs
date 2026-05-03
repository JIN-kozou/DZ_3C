using UnityEngine;

namespace DZ_3C.MachineRepair
{
    public static class RepairMeshUtility
    {
        //这个代码用来定义MachinePartCategory里面的枚举mesh形状。PartVisualShape
        private static Mesh _builtinSphere;
        private static Mesh _builtinCube;

        private static Mesh BuiltinSphere
        {
            get
            {
                if (_builtinSphere == null)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _builtinSphere = go.GetComponent<MeshFilter>().sharedMesh;
                    Object.Destroy(go);
                }

                return _builtinSphere;
            }
        }

        private static Mesh BuiltinCube
        {
            get
            {
                if (_builtinCube == null)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _builtinCube = go.GetComponent<MeshFilter>().sharedMesh;
                    Object.Destroy(go);
                }

                return _builtinCube;
            }
        }

        public static Mesh CreateTriangleMesh(float size = 1f)
        {
            float h = size * 0.866f;
            var m = new Mesh { name = "MachinePartTriangle" };
            m.vertices = new[]
            {
                new Vector3(0f, 0f, h * 2f / 3f),
                new Vector3(-size * 0.5f, 0f, -h / 3f),
                new Vector3(size * 0.5f, 0f, -h / 3f)
            };
            m.triangles = new[] { 0, 1, 2 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static void ApplyPartVisual(Transform root, PartVisualShape shape, MeshFilter mf, MeshRenderer mr)
        {
            if (mf == null || mr == null) return;
            root.localScale = Vector3.one;
            switch (shape)//在这里新增自生成mesh枚举
            {
                case PartVisualShape.Triangle:
                    mf.sharedMesh = CreateTriangleMesh(0.65f);
                    ApplyColor(mr, new Color(0.2f, 0.85f, 0.95f));
                    break;
                case PartVisualShape.Ellipse:
                    mf.sharedMesh = BuiltinSphere;
                    root.localScale = new Vector3(0.55f, 0.38f, 0.78f);
                    ApplyColor(mr, new Color(1f, 0.82f, 0.15f));
                    break;
                case PartVisualShape.Circle:
                    mf.sharedMesh = BuiltinSphere;
                    root.localScale = Vector3.one * 0.48f;
                    ApplyColor(mr, new Color(0.55f, 0.75f, 1f));
                    break;
            }
        }

        public static void ApplyReceiverBox(Transform root, MeshFilter mf, MeshRenderer mr, Vector3 scale)
        {
            if (mf == null || mr == null) return;
            mf.sharedMesh = BuiltinCube;
            root.localScale = scale;
            ApplyColor(mr, new Color(0.42f, 0.44f, 0.48f));
        }

        private static void ApplyColor(MeshRenderer mr, Color c)//在这里给自生成挂shader
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            mr.sharedMaterial = new Material(shader);
            if (mr.sharedMaterial.HasProperty("_BaseColor"))
            {
                mr.sharedMaterial.SetColor("_BaseColor", c);
            }
            else
            {
                mr.sharedMaterial.color = c;
            }
        }
    }
}

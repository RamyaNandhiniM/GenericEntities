using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GenericEntities
{
    internal class CDOMHelper
    {
        CodeCompileUnit targetUnit;
        CodeTypeDeclaration targetClass;

        public string ClassName { get; set; }

        public CDOMHelper(string typeName)
        {
            this.ClassName = typeName;
            targetUnit = new CodeCompileUnit();
            CodeNamespace nclcSpace = new CodeNamespace("DynamicEntities");
            nclcSpace.Imports.Add(new CodeNamespaceImport("System"));
            targetClass = new CodeTypeDeclaration(typeName);
            targetClass.IsClass = true;
            targetClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
            nclcSpace.Types.Add(targetClass);
            targetUnit.Namespaces.Add(nclcSpace);
        }

        public void AddProperty(string propName)
        {
            CodeMemberProperty dynamicProperty = new CodeMemberProperty();
            dynamicProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            dynamicProperty.Name = propName;
            dynamicProperty.HasGet = true;
            dynamicProperty.HasSet = true;
            dynamicProperty.Type = new CodeTypeReference(typeof(System.String));            
            targetClass.Members.Add(dynamicProperty);
        }

        public void GenerateType()
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "Block";
            
            using (StreamWriter sourceWriter = new StreamWriter(@"D:\testclass.cs"))
            {
                provider.GenerateCodeFromCompileUnit(targetUnit, sourceWriter, options);
            }
        }
    }
}

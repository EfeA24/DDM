using Microsoft.Data.Edm;

namespace Query.Api.OData
{
    public static class DynamicEdmModelFactory
    {
        public static IEdmModel CreateModel()
        {
            var model = new EdmModel();
            var entityType = new EdmEntityType("QueryService", "DynamicRow", baseType: null, isAbstract: false, isOpen: true);
            var key = entityType.AddStructuralProperty("__id", EdmPrimitiveTypeKind.String);
            entityType.AddKeys(key);
            model.AddElement(entityType);

            var container = new EdmEntityContainer("QueryService", "DefaultContainer");
            container.AddEntitySet("Rows", entityType);
            model.AddElement(container);

            return model;
        }
    }
}

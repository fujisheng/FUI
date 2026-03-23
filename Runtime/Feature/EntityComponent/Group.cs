using System;
using System.Collections.Generic;

namespace EntityComponent
{
    public class Group
    {
        public int Id { get; private set; }
        List<Entity> entities;

        public Group(int id)
        {
            Id = id;
            entities = new List<Entity>();
        }

        public static Group Default = new Group(0);

        public void CreateEntity(string name = "", params Type[] componentTypes)
        {
            var entity = new Entity(name, componentTypes);
        }
    }
}
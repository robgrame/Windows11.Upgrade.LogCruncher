using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogCruncher.Data
{
    public class HumanReadableOutputEntity
    {
        [Key]
        public int Id { get; set; }

        public int? RunInfosId { get; set; }
        [ForeignKey(nameof(RunInfosId))]
        public RunInfosEntity? RunInfos { get; set; }

        public ICollection<AssetEntity>? Assets { get; set; }
    }

    public class RunInfosEntity
    {
        [Key]
        public int Id { get; set; }

        public ICollection<RunInfoEntity>? RunInfo { get; set; }
    }

    public class RunInfoEntity
    {
        [Key]
        public int Id { get; set; }

        public ICollection<ComponentEntity>? Components { get; set; }
    }

    public class ComponentEntity
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Type { get; set; }

        [MaxLength(255)]
        public string? TypeIdentifier { get; set; }

        public ICollection<PropertyEntity>? Properties { get; set; }
    }

    public class AssetEntity
    {
        [Key]
        public int Id { get; set; }

        public ICollection<PropertyListEntity>? PropertyLists { get; set; }
    }

    public class PropertyListEntity
    {
        [Key]
        public int Id { get; set; }
        public string? ComputerName { get; set; }
        [MaxLength(255)]
        public string? Type { get; set; }
        public ICollection<PropertyEntity>? Properties { get; set; }
        public string Hash { get; set; }
    }

    public class PropertyEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string Value { get; set; }

        public int Ordinal { get; set; }
    }
}

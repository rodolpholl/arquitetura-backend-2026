using FinControl.Lancamentos.Core.Domain;
using FinControl.Lancamentos.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinControl.Lancamentos.Core.Context.DbMapping;

/// <summary>
/// Entity Framework Core mapping configuration for the Lancamento entity.
/// </summary>
public class LancamentoDbMapping : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos", schema: "lancamentos");

        // Primary Key
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        // Navigation ID (external reference - auto-generated UUID)
        builder.Property(l => l.NavigationId)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd()
            .HasColumnName("navigation_id");

        builder.HasIndex(l => l.NavigationId)
            .IsUnique()
            .HasDatabaseName("idx_lancamento_navigation_id");

        // Modalidade
        builder.Property(l => l.Modalidade)
            .HasConversion<int>()
            .IsRequired()
            .HasColumnName("modalidade");

        // Valor (in cents, stored as BIGINT)
        builder.Property(l => l.Valor)
            .IsRequired()
            .HasColumnName("valor");

        // Descrição (optional)
        builder.Property(l => l.Descricao)
            .HasMaxLength(500)
            .HasColumnName("descricao");

        // Data do Lançamento
        builder.Property(l => l.DataLancamento)
            .IsRequired()
            .HasColumnName("data_lancamento");

        // Computed properties - not mapped
        builder.Ignore(l => l.ValorFormatado);
        builder.Ignore(l => l.Tipo);
        builder.Ignore(l => l.TipoFormatado);

        // Audit properties (IAuditableDomainEntity)
        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasColumnName("criado_em");

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("atualizado_em");

        builder.Property(l => l.CreatedBy)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("criado_por");

        builder.Property(l => l.CreatedByName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("criado_por_nome");

        builder.Property(l => l.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("criado_por_email");

        builder.Property(l => l.UpdatedBy)
            .HasMaxLength(100)
            .HasColumnName("atualizado_por");

        builder.Property(l => l.UpdatedByName)
            .HasMaxLength(200)
            .HasColumnName("atualizado_por_nome");

        builder.Property(l => l.UpdatedByEmail)
            .HasMaxLength(200)
            .HasColumnName("atualizado_por_email");

        // Soft delete properties (ISoftDeleteDomainEntity)
        builder.Property(l => l.DeletedAt)
            .HasColumnName("deletado_em");

        builder.Property(l => l.DeletedBy)
            .HasMaxLength(100)
            .HasColumnName("deletado_por");

        // Indexes for query performance
        builder.HasIndex(l => l.DataLancamento)
            .HasDatabaseName("idx_lancamento_data");

        builder.HasIndex(l => new{l.DataLancamento, l.Modalidade})
            .HasDatabaseName("idx_lancamento_data_modalidade");

        builder.HasIndex(l => new { l.CreatedBy, l.DataLancamento })
            .HasDatabaseName("idx_lancamento_criado_por_data");

        builder.HasIndex(l => l.CreatedBy)
            .HasDatabaseName("idx_lancamento_criado_por");

        builder.HasIndex(l => l.DeletedAt)
            .HasDatabaseName("idx_lancamento_deletado");
    }
}

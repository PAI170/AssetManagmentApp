using AssetManagmentApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetManagmentApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<TipoEquipo> TiposEquipo => Set<TipoEquipo>();
    public DbSet<HistorialPrecioTipoEquipo> HistorialPreciosTipoEquipo => Set<HistorialPrecioTipoEquipo>();
    public DbSet<Activo> Activos => Set<Activo>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<AsignacionActivoProyecto> AsignacionesActivoProyecto => Set<AsignacionActivoProyecto>();
    public DbSet<Proforma> Proformas => Set<Proforma>();
    public DbSet<ProformaDetalle> ProformaDetalles => Set<ProformaDetalle>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Activo>()
            .HasIndex(a => a.Placa)
            .IsUnique();

        builder.Entity<Activo>()
            .HasOne(a => a.ProyectoActual)
            .WithMany(p => p.ActivosAsignados)
            .HasForeignKey(a => a.ProyectoActualId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Activo>()
            .HasOne(a => a.TipoEquipo)
            .WithMany(t => t.Activos)
            .HasForeignKey(a => a.TipoEquipoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialPrecioTipoEquipo>()
            .HasOne(h => h.TipoEquipo)
            .WithMany(t => t.HistorialPrecios)
            .HasForeignKey(h => h.TipoEquipoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Movimiento>()
            .HasOne(m => m.Activo)
            .WithMany(a => a.Movimientos)
            .HasForeignKey(m => m.ActivoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Movimiento>()
            .HasOne(m => m.Proyecto)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Movimiento>()
            .HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AsignacionActivoProyecto>()
            .HasOne(a => a.Activo)
            .WithMany(a => a.Asignaciones)
            .HasForeignKey(a => a.ActivoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AsignacionActivoProyecto>()
            .HasOne(a => a.Proyecto)
            .WithMany(p => p.Asignaciones)
            .HasForeignKey(a => a.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Proforma>()
            .HasIndex(p => p.Numero)
            .IsUnique();

        builder.Entity<Proforma>()
            .HasOne(p => p.Proyecto)
            .WithMany(p => p.Proformas)
            .HasForeignKey(p => p.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Proforma>()
            .HasOne(p => p.UsuarioGenero)
            .WithMany()
            .HasForeignKey(p => p.UsuarioGeneroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProformaDetalle>()
            .HasOne(d => d.Proforma)
            .WithMany(p => p.Detalles)
            .HasForeignKey(d => d.ProformaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProformaDetalle>()
            .HasOne(d => d.Activo)
            .WithMany(a => a.DetallesProforma)
            .HasForeignKey(d => d.ActivoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Proyecto>()
            .Property(p => p.Estado)
            .HasConversion<string>();

        builder.Entity<Activo>()
            .Property(a => a.Estado)
            .HasConversion<string>();

        builder.Entity<Movimiento>()
            .Property(m => m.TipoMovimiento)
            .HasConversion<string>();

        builder.Entity<Movimiento>()
            .Property(m => m.EstadoAnterior)
            .HasConversion<string>();

        builder.Entity<Movimiento>()
            .Property(m => m.EstadoNuevo)
            .HasConversion<string>();

        builder.Entity<Proforma>()
            .Property(p => p.Estado)
            .HasConversion<string>();
    }
}

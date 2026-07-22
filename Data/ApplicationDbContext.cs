using Alertas.Models;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Data
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Ciudad> Ciudades { get; set; }
        public virtual DbSet<Cliente> Clientes { get; set; }
        public virtual DbSet<Dominio> Dominios { get; set; }
        public virtual DbSet<Empresa> Empresas { get; set; }
        public virtual DbSet<Estado> Estados { get; set; }
        public virtual DbSet<GrupoAlerta> GruposAlertas { get; set; }
        public virtual DbSet<OblAdjunto> OblAdjuntos { get; set; }
        public virtual DbSet<JustifVar> JustifVars { get; set; }
        public virtual DbSet<Periodo> Periodos { get; set; }
        public virtual DbSet<RegObl> RegObls { get; set; }
        public virtual DbSet<Rol> Roles { get; set; }
        public virtual DbSet<TipoObligacion> TipoObligaciones { get; set; }
        public virtual DbSet<Usuario> Usuarios { get; set; }
        public virtual DbSet<RolEstadoTransicion> RolesEstadosTransicion { get; set; }
        public virtual DbSet<Area> Areas { get; set; }
        public virtual DbSet<AreaEmpresa> AreasEmpresas { get; set; }

        public virtual DbSet<Proyecto> Proyectos { get; set; }
        public virtual DbSet<UsuarioObligacion> UsuariosObligaciones { get; set; }
        public virtual DbSet<Mensaje> Mensajes { get; set; }
        public virtual DbSet<GrupoAlertaDia> GruposAlertasDias { get; set; }
        public virtual DbSet<GrupoAlertaDiaEstadoOff> GruposAlertasDiasEstadosOff { get; set; }
        public virtual DbSet<UsuarioProyecto> UsuariosProyectos { get; set; }
        public virtual DbSet<UsuarioArea> UsuarioArea { get; set; }
        public virtual DbSet<HistOblCampo> HistOblCampos { get; set; }
        public DbSet<EstadoTransicion> EstadosTransicion { get; set; }
        public DbSet<HistOblFlujo> HistOblFlujos { get; set; }
        public virtual DbSet<Festivo> Festivos { get; set; }
        public virtual DbSet<NotificacionEnvio> NotificacionesEnvios { get; set; }
        public virtual DbSet<NotificacionEnvioDetalle> NotificacionesEnviosDetalle { get; set; }
        public virtual DbSet<NotificacionLog> NotificacionesLog { get; set; }
        public DbSet<JobsEjecucion> JobsEjecuciones { get; set; }
        public DbSet<JobsLock> JobsLocks { get; set; }
        public virtual DbSet<ConfiguracionOperativa> ConfiguracionesOperativas { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // De preferencia manejar la conexión desde Program.cs / appsettings.json
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<JobsEjecucion>(entity =>
            {
                entity.ToTable("jobs_ejecuciones");
                entity.HasKey(e => e.id_job_ejecucion);
            });

            modelBuilder.Entity<JobsLock>(entity =>
            {
                entity.ToTable("jobs_locks");
                entity.HasKey(e => e.nombre_job);
            });

            modelBuilder.Entity<Ciudad>(entity =>
            {
                entity.HasKey(e => e.id_ciudad).HasName("ciudades_pkey");
                entity.Property(e => e.id_ciudad).UseIdentityAlwaysColumn();
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(e => e.id_cliente).HasName("clientes_pkey");
                entity.Property(e => e.id_cliente).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);
            });

            modelBuilder.Entity<Dominio>(entity =>
            {
                entity.HasKey(e => e.id_dominio).HasName("dominio_pkey");
                entity.Property(e => e.id_dominio).UseIdentityAlwaysColumn();
            });

            modelBuilder.Entity<Empresa>(entity =>
            {
                entity.HasKey(e => e.id_empresa).HasName("empresas_pkey");
                entity.Property(e => e.id_empresa).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);
            });

            modelBuilder.Entity<Estado>(entity =>
            {
                entity.HasKey(e => e.id_estado).HasName("estados_pkey");
                entity.Property(e => e.id_estado).UseIdentityAlwaysColumn();
                entity.Property(e => e.bloquea).HasDefaultValue(false);
                entity.Property(e => e.control_vencimiento).HasDefaultValue(false);
                entity.Property(e => e.control_seguimiento).HasDefaultValue(false);
                entity.Property(e => e.activo).HasDefaultValue(true);

                entity.HasOne(e => e.Proyecto)
                    .WithMany(p => p.Estados)
                    .HasForeignKey(e => e.id_proyecto)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.id_proyecto)
                    .HasDatabaseName("ix_estados_id_proyecto");

                entity.HasIndex(e => new { e.id_proyecto, e.orden })
                    .HasDatabaseName("ix_estados_orden");
            });

            modelBuilder.Entity<GrupoAlerta>(entity =>
            {
                entity.HasKey(e => e.id_grupo_alerta).HasName("grupos_alertas_pkey");
                entity.Property(e => e.id_grupo_alerta).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);

                entity.HasOne(e => e.Proyecto)
                    .WithMany(p => p.GruposAlertas)
                    .HasForeignKey(e => e.id_proyecto)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.id_proyecto)
                    .HasDatabaseName("ix_grupos_alertas_id_proyecto");

                entity.HasIndex(e => new { e.nombre, e.id_proyecto })
                    .IsUnique()
                    .HasDatabaseName("uq_grupos_alertas_nombre_proyecto");
            });

            modelBuilder.Entity<OblAdjunto>(entity =>
            {
                entity.HasKey(e => e.id_obl_adjunto).HasName("imp_adjuntos_pkey");

                entity.Property(e => e.id_obl_adjunto)
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.fecha_carga)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.activo)
                    .HasDefaultValue(true);

                entity.Property(e => e.eliminado)
                    .HasDefaultValue(false);

                entity.Property(e => e.tipo_soporte)
                    .HasMaxLength(50)
                    .HasDefaultValue("NORMAL");

                entity.HasOne(e => e.UsuarioEliminacion)
                    .WithMany()
                    .HasForeignKey(e => e.id_usuario_eliminacion)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.eliminado_fisicamente)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<JustifVar>(entity =>
            {
                entity.HasKey(e => e.id_justif_var).HasName("justif_var_pkey");
                entity.Property(e => e.id_justif_var).UseIdentityAlwaysColumn();
            });

            modelBuilder.Entity<Periodo>(entity =>
            {
                entity.HasKey(e => e.id_periodo).HasName("periodos_pkey");
                entity.Property(e => e.id_periodo).UseIdentityAlwaysColumn();
            });


            modelBuilder.Entity<Proyecto>(entity =>
            {
                entity.HasKey(e => e.id_proyecto).HasName("proyectos_pkey");
                entity.Property(e => e.id_proyecto).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);
                entity.Property(e => e.fecha_creacion).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Area)
                    .WithMany(a => a.Proyectos)
                    .HasForeignKey(e => e.id_area)
                    .HasPrincipalKey(a => a.id_area)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UsuarioCreacion)
                    .WithMany(u => u.ProyectosCreados)
                    .HasForeignKey(e => e.id_usuario_creacion)
                    .HasPrincipalKey(u => u.id_usuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.id_area)
                    .HasDatabaseName("ix_proyectos_id_area");

                entity.HasIndex(e => new { e.nombre, e.id_area })
                    .IsUnique()
                    .HasDatabaseName("uq_proyectos_nombre_area");
            });

            modelBuilder.Entity<RegObl>(entity =>
            {
                entity.HasKey(e => e.id_reg_obl).HasName("reg_obl_pkey");
                entity.Property(e => e.id_reg_obl).UseIdentityAlwaysColumn();

                entity.HasOne(r => r.Proyecto)
                    .WithMany(p => p.Obligaciones)
                    .HasForeignKey(r => r.id_proyecto)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Estado)
                    .WithMany(e => e.Obligaciones)
                    .HasForeignKey(r => r.id_estado)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.AutorizadoPor)
                    .WithMany(u => u.RegImpsComoAutorizador)
                    .HasForeignKey(r => r.id_autorizado_por)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.AprobadoPor)
                    .WithMany(u => u.RegImpsComoAprobador)
                    .HasForeignKey(r => r.id_aprobado_por)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.id_proyecto)
                    .HasDatabaseName("ix_reg_obl_id_proyecto");

                entity.HasIndex(e => e.id_estado)
                    .HasDatabaseName("ix_reg_obl_id_estado");
            });

            modelBuilder.Entity<Rol>(entity =>
            {
                entity.HasKey(e => e.id_rol).HasName("roles_pkey");
                entity.Property(e => e.id_rol).UseIdentityAlwaysColumn();
                entity.Property(e => e.Activo).HasDefaultValue(true);
            });

            modelBuilder.Entity<TipoObligacion>(entity =>
            {
                entity.HasKey(e => e.id_tipo_obligacion).HasName("tip_imp_pkey");
                entity.Property(e => e.id_tipo_obligacion).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.id_usuario).HasName("usuarios_pkey");
                entity.Property(e => e.id_usuario).UseIdentityAlwaysColumn();
            });

            modelBuilder.Entity<Area>(entity =>
            {
                entity.HasKey(e => e.id_area).HasName("areas_pkey");
                entity.Property(e => e.id_area).UseIdentityAlwaysColumn();
            });

            modelBuilder.Entity<AreaEmpresa>(entity =>
            {
                entity.HasKey(e => e.id_area_empresa).HasName("area_empresa_pkey");
                entity.Property(e => e.id_area_empresa).UseIdentityAlwaysColumn();
            });

            modelBuilder.Entity<UsuarioObligacion>(entity =>
            {
                entity.HasKey(e => e.id_usuario_obligacion).HasName("usuarios_obligaciones_pkey");
                entity.Property(e => e.id_usuario_obligacion).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);
                entity.Property(e => e.fecha_asignacion).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.UsuariosObligaciones)
                    .HasForeignKey(e => e.id_usuario)
                    .HasPrincipalKey(u => u.id_usuario)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UsuarioAsignacion)
                    .WithMany(u => u.UsuariosObligacionesAsignadas)
                    .HasForeignKey(e => e.id_usuario_asignacion)
                    .HasPrincipalKey(u => u.id_usuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RegObl)
                    .WithMany(r => r.UsuariosObligaciones)
                    .HasForeignKey(e => e.id_reg_obl)
                    .HasPrincipalKey(r => r.id_reg_obl)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Rol)
                    .WithMany(r => r.UsuariosObligaciones)
                    .HasForeignKey(e => e.id_rol)
                    .HasPrincipalKey(r => r.id_rol)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.id_usuario)
                    .HasDatabaseName("ix_uo_id_usuario");

                entity.HasIndex(e => e.id_reg_obl)
                    .HasDatabaseName("ix_uo_id_reg_obl");

                entity.HasIndex(e => e.id_rol)
                    .HasDatabaseName("ix_uo_id_rol");

                entity.HasIndex(e => new { e.id_usuario, e.id_reg_obl, e.id_rol })
                    .IsUnique()
                    .HasDatabaseName("uq_uo_usuario_obligacion_rol");
            });

            modelBuilder.Entity<Mensaje>(entity =>
            {
                entity.HasKey(e => e.id_mensaje).HasName("mensajes_pkey");
                entity.Property(e => e.id_mensaje).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);

                entity.HasCheckConstraint("chk_mensajes_prioridad", "prioridad between 1 and 3");
            });

            modelBuilder.Entity<GrupoAlertaDia>(entity =>
            {
                entity.HasKey(e => e.id_grupo_alerta_dia).HasName("grupos_alertas_dias_pkey");
                entity.Property(e => e.id_grupo_alerta_dia).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);

                entity.HasOne(e => e.GrupoAlerta)
                    .WithMany(g => g.GruposAlertasDias)
                    .HasForeignKey(e => e.id_grupo_alerta)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Rol)
                    .WithMany(r => r.GruposAlertasDias)
                    .HasForeignKey(e => e.id_rol)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Mensaje)
                    .WithMany(m => m.GruposAlertasDias)
                    .HasForeignKey(e => e.id_mensaje)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Dependencia)
                    .WithMany(d => d.Dependientes)
                    .HasForeignKey(e => e.id_dependencia)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.id_grupo_alerta)
                    .HasDatabaseName("ix_gad_id_grupo_alerta");

                entity.HasIndex(e => e.id_rol)
                    .HasDatabaseName("ix_gad_id_rol");

                entity.HasIndex(e => e.id_mensaje)
                    .HasDatabaseName("ix_gad_id_mensaje");

                entity.HasIndex(e => new
                {
                    e.id_grupo_alerta,
                    e.tipo_control,
                    e.operador,
                    e.valor_dias,
                    e.id_rol
                })
                .IsUnique()
                .HasDatabaseName("uq_gad_regla");
            });

            modelBuilder.Entity<GrupoAlertaDiaEstadoOff>(entity =>
            {
                entity.HasKey(e => e.id_grupo_alerta_dia_estado_off)
                    .HasName("grupos_alertas_dias_estados_off_pkey");

                entity.Property(e => e.id_grupo_alerta_dia_estado_off)
                    .UseIdentityAlwaysColumn();

                entity.HasOne(e => e.GrupoAlertaDia)
                    .WithMany(g => g.EstadosOff)
                    .HasForeignKey(e => e.id_grupo_alerta_dia)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Estado)
                    .WithMany(e => e.GruposAlertasDiasEstadosOff)
                    .HasForeignKey(e => e.id_estado)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.id_grupo_alerta_dia, e.id_estado })
                    .IsUnique()
                    .HasDatabaseName("uq_gadeo_gad_estado");
            });

            modelBuilder.Entity<UsuarioProyecto>(entity =>
            {
                entity.HasKey(e => e.id_usuario_proyecto).HasName("usuarios_proyectos_pkey");
                entity.Property(e => e.id_usuario_proyecto).UseIdentityAlwaysColumn();
                entity.Property(e => e.activo).HasDefaultValue(true);
                entity.Property(e => e.fecha_asignacion).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.UsuariosProyectos)
                    .HasForeignKey(e => e.id_usuario)
                    .HasPrincipalKey(u => u.id_usuario)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Proyecto)
                    .WithMany(p => p.UsuariosProyectos)
                    .HasForeignKey(e => e.id_proyecto)
                    .HasPrincipalKey(p => p.id_proyecto)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Rol)
                    .WithMany(r => r.UsuariosProyectos)
                    .HasForeignKey(e => e.id_rol)
                    .HasPrincipalKey(r => r.id_rol)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UsuarioAsignacion)
                    .WithMany(u => u.UsuariosProyectosAsignados)
                    .HasForeignKey(e => e.id_usuario_asignacion)
                    .HasPrincipalKey(u => u.id_usuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.id_usuario)
                    .HasDatabaseName("ix_up_id_usuario");

                entity.HasIndex(e => e.id_proyecto)
                    .HasDatabaseName("ix_up_id_proyecto");

                entity.HasIndex(e => e.id_rol)
                    .HasDatabaseName("ix_up_id_rol");

                entity.HasIndex(e => new { e.id_usuario, e.id_proyecto, e.id_rol })
                    .IsUnique()
                    .HasDatabaseName("uq_up_usuario_proyecto_rol");
            });

            modelBuilder.Entity<EstadoTransicion>()
                .HasOne(et => et.EstadoOrigen)
                .WithMany(e => e.TransicionesComoOrigen)
                .HasForeignKey(et => et.id_estado_origen)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EstadoTransicion>()
                .HasOne(et => et.EstadoDestino)
                .WithMany(e => e.TransicionesComoDestino)
                .HasForeignKey(et => et.id_estado_destino)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EstadoTransicion>()
                .HasOne(et => et.Proyecto)
                .WithMany(p => p.EstadosTransicion)
                .HasForeignKey(et => et.id_proyecto)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolEstadoTransicion>()
                .HasOne(ret => ret.EstadoTransicion)
                .WithMany(et => et.RolesEstadosTransicion)
                .HasForeignKey(ret => ret.id_estado_transicion)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolEstadoTransicion>()
                .HasOne(ret => ret.Rol)
                .WithMany(r => r.RolesEstadosTransicion)
                .HasForeignKey(ret => ret.id_rol)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EstadoTransicion>()
                .HasIndex(et => new { et.id_proyecto, et.id_estado_origen, et.id_estado_destino })
                .IsUnique();

            modelBuilder.Entity<RolEstadoTransicion>()
                .HasIndex(ret => new { ret.id_estado_transicion, ret.id_rol })
                .IsUnique();

            modelBuilder.Entity<HistOblFlujo>()
                .HasOne(h => h.EstadoOrigen)
                .WithMany()
                .HasForeignKey(h => h.id_estado_origen)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistOblFlujo>()
                .HasOne(h => h.EstadoDestino)
                .WithMany()
                .HasForeignKey(h => h.id_estado_destino)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistOblFlujo>()
                .HasOne(h => h.RegObl)
                .WithMany()
                .HasForeignKey(h => h.id_reg_obl)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HistOblFlujo>()
                .HasOne(h => h.Usuario)
                .WithMany()
                .HasForeignKey(h => h.id_usuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegObl>()
                .HasOne(r => r.UsuarioSoportePostCierre)
                .WithMany()
                .HasForeignKey(r => r.id_usuario_soporte_post_cierre)
                .HasConstraintName("fk_reg_obl_usuario_soporte_post_cierre")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConfiguracionOperativa>(entity =>
            {
                entity.ToTable("configuracion_operativa");

                entity.HasKey(e => e.clave);

                entity.Property(e => e.clave)
                    .HasMaxLength(100);

                entity.Property(e => e.valor)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.descripcion)
                    .HasMaxLength(500);

                entity.HasOne(e => e.UsuarioActualizacion)
                    .WithMany()
                    .HasForeignKey(e => e.id_usuario_actualizacion)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
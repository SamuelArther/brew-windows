namespace Brew.Ruby;

// `post_install_steps do ... end`: Homebrew's declarative post-install DSL (install_steps.rb), run
// directly after install. Steps that only mean something on macOS/Linux (keychains, dylib ids,
// ownership, glibc/gcc runtime setup) are no-ops here.
public static partial class FormulaLoader
{
    internal static bool HasPostInstallSteps(FormulaSpec spec) =>
        spec.Class.GetIvar("@__post_install_steps") is RArray { L.Count: > 0 };

    const string InstallStepsPrelude = """
        module Homebrew
          module InstallSteps
            class DSL
              TOKENS = %w[prefix opt_prefix bin sbin lib libexec share pkgshare var etc pkgetc
                          bash_completion zsh_completion fish_completion].freeze

              def initialize(formula)
                @f = formula
                @skip = 0
              end

              def name = @f.name
              def formula_name = @f.name
              def version = @f.version

              def base_path(base, formula = nil)
                f = formula ? Formula[formula] : @f
                case (base || :prefix).to_s
                when "prefix" then formula ? f.opt_prefix : @f.prefix
                when "opt_prefix" then f.opt_prefix
                when "homebrew_prefix" then HOMEBREW_PREFIX
                when "etc" then HOMEBREW_PREFIX/"etc"
                when "var" then HOMEBREW_PREFIX/"var"
                when "pkgetc" then HOMEBREW_PREFIX/"etc"/f.name
                when "temp" then Pathname(ENV.fetch("TEMP", Dir.pwd))
                else f.send(base.to_s)
                end
              end

              def expand(text)
                text.to_s.gsub(/\{\{(\w+)\}\}/) do
                  token = Regexp.last_match(1)
                  TOKENS.include?(token) ? base_path(token).to_s : "{{#{token}}}"
                end
              end

              def resolve(path, base = nil, formula = nil)
                path = expand(path)
                return Pathname(path) if path.start_with?("/", "~") || path.match?(%r{\A[A-Za-z]:[/\\]})

                base_path(base, formula)/path
              end

              def mkdir_p(path, base: nil) = resolve(path, base).mkpath
              def mkdir(path, base: nil) = mkdir_p(path, base:)

              def touch(path, base: nil)
                file = resolve(path, base)
                file.dirname.mkpath
                FileUtils.touch file
              end

              def symlink(source, target, source_base: nil, target_base: nil, source_formula: nil, target_formula: nil,
                          force: false, overwrite: false, **_)
                src = resolve(source, source_base, source_formula)
                dst = resolve(target, target_base, target_formula)
                return if (dst.exist? || dst.symlink?) && !(force || overwrite)

                dst.dirname.mkpath
                FileUtils.ln_sf src, dst
              end

              def ln_s(source, target, **opts) = symlink(source, target, **opts)
              def ln_sf(source, target, **opts) = symlink(source, target, **opts, overwrite: true)

              def symlink_children(source, target = nil, source_base: nil, target_base: :homebrew_prefix, prefix: "", suffix: "", **_)
                src = resolve(source, source_base)
                return unless src.directory?

                dst = resolve(target || source, target_base)
                src.children.each do |child|
                  symlink child.to_s, (dst/"#{prefix}#{child.basename}#{suffix}").to_s, overwrite: true
                end
              end

              def link_children(source, target = nil, **opts) = symlink_children(source, target, **opts)

              def symlink_tree(source, target, source_base: nil, target_base: :homebrew_prefix)
                src = resolve(source, source_base)
                return unless src.directory?

                dst = resolve(target, target_base)
                Dir.glob("#{src}/**/*").each do |file|
                  next if File.directory?(file)

                  rel = Pathname(file).relative_path_from(src)
                  symlink file, (dst/rel).to_s, overwrite: true
                end
              end

              def link_dir(source, target, source_base: nil, target_base: :homebrew_prefix)
                symlink source, target, source_base:, target_base:, overwrite: true
              end

              def copy(source, target, source_base: nil, target_base: nil, recursive: false, overwrite: true, **_)
                src = resolve(source, source_base)
                dst = resolve(target, target_base)
                return if dst.exist? && !overwrite

                dst.dirname.mkpath
                recursive ? FileUtils.cp_r(src, dst) : FileUtils.cp(src, dst)
              end

              def move(source, target, source_base: nil, target_base: nil, overwrite: true, **_)
                src = resolve(source, source_base)
                dst = resolve(target, target_base)
                return unless src.exist?
                return if dst.exist? && !overwrite

                dst.dirname.mkpath
                FileUtils.mv src, dst, force: true
              end

              def mv(source, target, **opts) = move(source, target, **opts)

              def remove(paths, base: nil, recursive: false, **_)
                Array(paths).each do |path|
                  file = resolve(path, base)
                  recursive ? FileUtils.rm_rf(file) : FileUtils.rm_f(file)
                end
              end

              def inreplace(path, before, after, base: nil, global: true, **_)
                file = resolve(path, base)
                text = file.read
                replaced = global ? text.gsub(before, after) : text.sub(before, after)
                file.write(replaced) if replaced != text
              end

              def write(path, content, base: nil, overwrite: false)
                file = resolve(path, base)
                return if file.exist? && !overwrite

                file.dirname.mkpath
                File.write(file, expand(content))
              end

              def write_file(path, content, base: nil, overwrite: true, append_newline: false)
                text = expand(content)
                text += "\n" if append_newline && !text.end_with?("\n")
                write(path, text, base:, overwrite:)
              end

              def run(command, args: [], base: nil, env: {}, must_succeed: true, stdout_path: nil, chdir: nil, **_)
                cmd = base ? resolve(command, base).to_s : expand(command)
                argv = [cmd, *args.map { |a| expand(a) }]
                dir = chdir ? resolve(chdir).to_s : Dir.pwd
                ok = Dir.chdir(dir) do
                  with_env(env.transform_keys(&:to_s)) do
                    if stdout_path
                      output = Utils.safe_popen_read(*argv)
                      resolve(stdout_path).write(output)
                      true
                    else
                      system(*argv)
                    end
                  end
                end
                raise "post-install step failed: #{argv.join(" ")}" if !ok && must_succeed
              end

              def warn(message) = opoo(message)

              def if_path_exists(path, base: nil, &block)
                instance_exec(&block) if resolve(path, base).exist?
              end

              def unless_path_exists(path, base: nil, &block)
                instance_exec(&block) unless resolve(path, base).exist?
              end

              # Windows is neither; those blocks are skipped.
              def on_macos(&_block) = nil
              def on_linux(&_block) = nil

              def init_data_dir(path, using:, base: nil, locale: nil)
                opoo "#{@f.name}: initialise the data directory #{resolve(path, base)} yourself (#{using})"
              end

              # Caches that only matter when the GTK/GLib tools are installed.
              def compile_gsettings_schemas = refresh("glib-compile-schemas", HOMEBREW_PREFIX/"share/glib-2.0/schemas")
              def update_gtk_icon_cache = refresh("gtk-update-icon-cache", "-qtf", HOMEBREW_PREFIX/"share/icons/hicolor")
              def gtk_update_icon_cache = update_gtk_icon_cache
              def update_mime_database = refresh("update-mime-database", HOMEBREW_PREFIX/"share/mime")
              def update_desktop_database = refresh("update-desktop-database", HOMEBREW_PREFIX/"share/applications")
              def gio_querymodules = nil
              def update_gio_modules_cache = nil
              def gdk_pixbuf_query_loaders = nil
              def update_gdk_pixbuf_loaders_cache = nil

              def refresh(tool, *args)
                program = HOMEBREW_PREFIX/"bin"/tool
                return unless program.exist? || (HOMEBREW_PREFIX/"bin/#{tool}.exe").exist?
                return unless args.last.is_a?(Pathname) && args.last.exist?

                system program.to_s, *args.map(&:to_s)
              end

              def set_permissions(*_, **_) = nil
              def set_ownership(*_, **_) = nil
              def change_dylib_id(*_, **_) = nil
              def delete_keychain_certificate(*_, **_) = nil
              def delete_keychain_certificates(*_, **_) = nil
              def terminate_process(*_, **_) = nil
              def configure_gcc_runtime = nil
              def configure_glibc_runtime = nil
              def configure_clang_system = nil
              def configure_php = nil
              def bootstrap_cpython = nil
              def bootstrap_pypy(**_) = nil

              def install_gzipped_executable(source, target, source_base: nil, target_base: nil)
                src = resolve(source, source_base)
                dst = resolve(target, target_base)
                dst.dirname.mkpath
                system "gzip", "-dc", src.to_s, out: dst.to_s
              end
            end
          end
        end

        # ActiveSupport's blank?/present?, which Homebrew loads.
        class Object
          def blank? = respond_to?(:empty?) ? !!empty? : !self
          def present? = !blank?
        end

        class NilClass
          def blank? = true
        end

        class String
          def blank? = strip.empty?
        end

        class << ENV
          def prepend_create_path(key, path)
            path = Pathname(path)
            path.mkpath
            prepend_path(key, path)
          end
        end

        class Formula
          def self.post_install_steps(*_steps, &block)
            (@__post_install_steps ||= []) << block if block
            @__post_install_steps || []
          end

          def __run_post_install_steps
            dsl = Homebrew::InstallSteps::DSL.new(self)
            self.class.instance_variable_get(:@__post_install_steps).to_a.each { |block| dsl.instance_exec(&block) }
          end
        end
        """;
}

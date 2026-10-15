class Rust < Formula
  desc "Safe, concurrent, practical language"
  homepage "https://www.rust-lang.org/"
  url "https://static.rust-lang.org/dist/rust-1.99.0-x86_64-pc-windows-msvc.tar.xz"
  sha256 "209f916c04cd7ed1938a4592e8adc1b1017adada67dc8f8a0a92bca9c4963966"
  license any_of: ["Apache-2.0", "MIT"]

  livecheck do
    url "https://static.rust-lang.org/dist/channel-rust-stable.toml"
    regex(/^version\s*=\s*"(\d+(?:\.\d+)+)/i)
  end

  # Official standalone Rust toolchain for x86_64-pc-windows-msvc. Instead of running its
  # install.sh, the component trees are merged into libexec (a standard Rust sysroot).
  # Linking executables needs the MSVC build tools (link.exe) on the system.

  def install
    %w[
      rustc rust-std-x86_64-pc-windows-msvc cargo clippy-preview rustfmt-preview rust-analyzer-preview
    ].each do |component|
      install_tree component, "libexec"
    end
    %w[cargo cargo-clippy cargo-fmt clippy-driver rust-analyzer rustc rustdoc rustfmt].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
    end
  end

  test do
    system bin/"rustdoc", "-h"
    (testpath/"hello.rs").write <<~RUST
      fn main() {
        println!("Hello World!");
      }
    RUST
    system bin/"rustc", "hello.rs"
    assert_equal "Hello World!\n", shell_output("./hello")
    assert_match version.to_s, shell_output("#{bin}/cargo --version")
  end
end

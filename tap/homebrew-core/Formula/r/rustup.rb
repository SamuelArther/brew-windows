class Rustup < Formula
  desc "Rust toolchain installer"
  homepage "https://rust-lang.github.io/rustup/"
  url "https://static.rust-lang.org/rustup/archive/1.29.1/x86_64-pc-windows-msvc/rustup-init.exe"
  sha256 "6f4bef66261261fcb43131be8720bab817d403a09edec7455c371974b90bdb7e"
  license any_of: ["Apache-2.0", "MIT"]
  revision 1

  livecheck do
    url "https://static.rust-lang.org/rustup/release-stable.toml"
    regex(/^version\s*=\s*"(\d+(?:\.\d+)+)/i)
  end

  keg_only "it conflicts with rust"

  # Official rustup-init for x86_64-pc-windows-msvc. A copy named rustup.exe acts as the
  # rustup proxy (rustup dispatches on its executable name), like homebrew-core's symlinks.

  def install
    bin.install "rustup-init.exe"
    bin.install "rustup-init.exe" => "rustup.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rustup-init --version")
  end
end

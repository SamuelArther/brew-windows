class Uv < Formula
  desc "Extremely fast Python package installer and resolver, written in Rust"
  homepage "https://docs.astral.sh/uv/"
  url "https://github.com/astral-sh/uv/releases/download/0.13.1/uv-x86_64-pc-windows-msvc.zip"
  sha256 "3b2661a463a5baea7f5076ea2b25ab71d77f36a6c0d809e5350651792f80a672"
  license any_of: ["Apache-2.0", "MIT"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/uv.exe", "uv"
    windows_exe "libexec/uvx.exe", "uvx"
    windows_exe "libexec/uvw.exe", "uvw"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/uv --version")
  end
end

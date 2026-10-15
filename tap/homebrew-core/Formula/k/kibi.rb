class Kibi < Formula
  desc "Text editor in ≤1024 lines of code, written in Rust"
  homepage "https://github.com/ilai-deutel/kibi"
  url "https://github.com/ilai-deutel/kibi/releases/download/v0.3.3/kibi-v0.3.3-x86_64-pc-windows-gnu.zip"
  sha256 "38e8e31fbdf6026a30d04e0e3511c5594bf853f30f57312805cd49571294bdf8"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ilai-deutel/kibi/archive/refs/tags/v0.3.3.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/kibi.exe", "kibi"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kibi --version 2>&1")
  end
end

class Flawz < Formula
  desc "Terminal UI for browsing security vulnerabilities (CVEs)"
  homepage "https://github.com/orhun/flawz"
  url "https://github.com/orhun/flawz/releases/download/v0.4.1/flawz-x86_64-pc-windows-msvc.zip"
  sha256 "06211cd87e3159955f01628a9494250f9f32154dd78759d77e8ba76cc3f11f99"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/orhun/flawz/archive/refs/tags/v0.4.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/flawz.exe", "flawz"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/flawz --version 2>&1")
  end
end

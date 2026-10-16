class Hwatch < Formula
  desc "Modern alternative to the watch command"
  homepage "https://github.com/blacknon/hwatch"
  url "https://github.com/blacknon/hwatch/releases/download/0.4.2/hwatch-0.4.2.x86_64-pc-windows-msvc.zip"
  sha256 "58b284e3d808a9c916320996988d73625778d590bc90c6029327f7d86cd2a608"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/blacknon/hwatch/archive/refs/tags/0.4.2.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/x86_64-pc-windows-msvc/release/hwatch.exe", "hwatch"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hwatch --version 2>&1")
  end
end

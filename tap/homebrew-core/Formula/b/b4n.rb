class B4n < Formula
  desc "Terminal user interface (TUI) for Kubernetes API written in Rust"
  homepage "https://github.com/fioletoven/b4n"
  url "https://github.com/fioletoven/b4n/releases/download/v1.1.0/b4n-1.1.0-x86_64-windows.zip"
  sha256 "21f1ed66d092daa9e00bb08c94d1729c0a44b38ed8c7e547840c679412e22f12"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fioletoven/b4n/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/b4n.exe", "b4n"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/b4n --version 2>&1")
  end
end

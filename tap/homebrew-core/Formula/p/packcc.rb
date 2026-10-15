class Packcc < Formula
  desc "Parser generator for C"
  homepage "https://github.com/arithy/packcc"
  url "https://github.com/arithy/packcc/releases/download/v3.1.0/packcc-v3.1.0-windows-2025.tar.gz"
  sha256 "99658b4d30db21de5788ce2d75b5c52f11ca617e9a42e85b723defc41464d40d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/arithy/packcc/archive/refs/tags/v3.1.0.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/packcc.exe", "packcc"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/packcc --version 2>&1")
  end
end

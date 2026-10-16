class VitePlus < Formula
  desc "Unified toolchain and entry point for web development"
  homepage "https://viteplus.dev"
  url "https://github.com/voidzero-dev/vite-plus/releases/download/v1.2.0/vp-x86_64-pc-windows-msvc.zip"
  sha256 "473ad107592cf82dd3c4bc1ff1f2d93e45605b858e2e5af3b332fdf2450af820"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/voidzero-dev/vite-plus/archive/refs/tags/v1.2.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/vp.exe", "vp"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vp --version 2>&1")
  end
end

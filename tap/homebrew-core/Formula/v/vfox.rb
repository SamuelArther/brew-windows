class Vfox < Formula
  desc "Version manager with support for Java, Node.js, Flutter, .NET & more"
  homepage "https://vfox.dev/"
  url "https://github.com/version-fox/vfox/releases/download/v1.0.12/vfox_1.0.12_windows_x86_64.zip"
  sha256 "0dd8285f636419a538a65b1a2bb7242e6fac5f50d9fdf3b479f517610846110e"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/version-fox/vfox/archive/refs/tags/v1.0.12.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "vfox.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vfox --version")

    system bin/"vfox", "add", "golang"
    output = shell_output("#{bin}/vfox info golang")
    assert_match "Golang plugin, https://go.dev/dl/", output
  end
end

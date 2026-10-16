class GoCamo < Formula
  desc "Secure image proxy server"
  homepage "https://github.com/cactus/go-camo"
  url "https://github.com/cactus/go-camo/releases/download/v2.7.7/go-camo-2.7.7.go1272.windows-amd64.tar.gz"
  sha256 "fde652e87fe47962cce6d132f7178b9e0ac0f746a3db018a34669b2b63017521"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cactus/go-camo/archive/refs/tags/v2.7.7.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/go-camo.exe", "go-camo"
    windows_exe "libexec/bin/url-tool.exe", "url-tool"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/go-camo --version 2>&1")
  end
end

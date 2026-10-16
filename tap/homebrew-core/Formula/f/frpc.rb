class Frpc < Formula
  desc "Client app of fast reverse proxy to expose a local server to the internet"
  homepage "https://github.com/fatedier/frp"
  url "https://github.com/fatedier/frp/releases/download/v0.71.1/frp_0.71.1_windows_amd64.zip"
  sha256 "b90074b2b6a1d792a7a40f085ac6d276bcab2d1bc60201f8a86ca53ce65ce821"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fatedier/frp/archive/refs/tags/v0.71.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/frpc.exe", "frpc"
  end

  test do
    assert_path_exists bin/"frpc.exe"
  end
end

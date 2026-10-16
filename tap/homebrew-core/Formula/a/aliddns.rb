class Aliddns < Formula
  desc "Aliyun(Alibaba Cloud) ddns for golang"
  homepage "https://github.com/OpenIoTHub/aliddns"
  url "https://github.com/OpenIoTHub/aliddns/releases/download/v0.0.23/aliddns_0.0.23_windows_amd64.tar.gz"
  sha256 "5c1ade23d5099dd2520407df59fe96a3c2a8ba5cbd086c80a01fd3f012a5b7fd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/OpenIoTHub/aliddns/archive/refs/tags/v0.0.23.tar.gz"
  end

  def install
    bin.install "aliddns.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aliddns -v 2>&1")
    assert_match "config created", shell_output("#{bin}/aliddns init --config=aliddns.yml 2>&1")
    assert_path_exists testpath/"aliddns.yml"
  end
end

class ConsulBackinator < Formula
  desc "Consul backup and restoration application"
  homepage "https://github.com/myENA/consul-backinator"
  url "https://github.com/myENA/consul-backinator/releases/download/v1.6.6/consul-backinator-1.6.6-amd64-windows.tar.gz"
  sha256 "e46d249036f623bd746f903a104935550f2ac89e9ad1dbff6d94f462a674c7d8"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/myENA/consul-backinator/archive/refs/tags/v1.6.6.tar.gz"
  end

  def install
    bin.install "consul-backinator-1.6.6.exe" => "consul-backinator.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/consul-backinator --version 2>&1")
  end
end

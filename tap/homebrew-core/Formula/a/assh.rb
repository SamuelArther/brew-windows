class Assh < Formula
  desc "Advanced SSH config - Regex, aliases, gateways, includes and dynamic hosts"
  homepage "https://v1.manfred.life/assh/"
  url "https://github.com/moul/assh/releases/download/v2.17.3/assh_2.17.3_windows_amd64.tar.gz"
  sha256 "87620490fd8a6a33396101dce1aea67f8d8b176a195a3500d3101128a7a403d4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/moul/assh/archive/refs/tags/v2.17.3.tar.gz"
  end

  def install
    bin.install "assh.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/assh --version 2>&1")
  end
end

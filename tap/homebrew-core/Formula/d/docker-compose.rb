class DockerCompose < Formula
  desc "Isolated development environments using Docker"
  homepage "https://docs.docker.com/compose/"
  version "5.6.0"
  url "https://github.com/docker/compose/releases/download/v5.6.0/docker-compose-windows-x86_64.exe"
  sha256 "8aeaa0055e660e12cf07cd70680dc8333957fe6718528b91ec3da084eab62483"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "docker-compose-windows-x86_64.exe" => "docker-compose.exe"
  end

  test do
    output = shell_output("#{bin}/docker-compose up 2>&1", 1)
    assert_match "no configuration file provided", output

    assert_match version.to_s, shell_output("#{bin}/docker-compose version")
  end
end

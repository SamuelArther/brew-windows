class DockerBuildx < Formula
  desc "Docker CLI plugin for extended build capabilities with BuildKit"
  homepage "https://docs.docker.com/buildx/working-with-buildx/"
  version "0.38.0"
  url "https://github.com/docker/buildx/releases/download/v0.38.0/buildx-v0.38.0.windows-amd64.exe"
  sha256 "b39784aed5b3f9b24d1971bbcbad8c8206033fa63ed4fea82c9141728fe0ea26"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "buildx-v0.38.0.windows-amd64.exe" => "docker-buildx.exe"
  end

  test do
    assert_match "github.com/docker/buildx v#{version}", shell_output("#{bin}/docker-buildx version")
  end
end

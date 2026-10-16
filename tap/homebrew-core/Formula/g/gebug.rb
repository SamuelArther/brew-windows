class Gebug < Formula
  desc "Debug Dockerized Go applications better"
  homepage "https://github.com/moshebe/gebug"
  url "https://github.com/moshebe/gebug/releases/download/v1.0.7/gebug_1.0.7_Windows_x86_64.tar.gz"
  sha256 "3e7a5b31e355977e7828aa45e549a194e11218d033f5a3930535b0da131cca84"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/moshebe/gebug/archive/refs/tags/v1.0.7.tar.gz"
  end

  def install
    bin.install "gebug.exe"
  end

  test do
    ENV["DOCKER_HOST"] = "unix://#{testpath}/invalid.sock"

    (testpath/".gebug/docker-compose.yml").write("")
    (testpath/".gebug/Dockerfile").write("")

    assert_match "Failed to perform clean up", shell_output("#{bin}/gebug clean 2>&1", 1)
    assert_match version.to_s, shell_output("#{bin}/gebug version")
  end
end

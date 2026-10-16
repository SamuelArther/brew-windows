class Socktainer < Formula
  desc "Docker-compatible REST API on top of Windows containers (Docker Engine)"
  homepage "https://socktainer.github.io"
  url "https://download.docker.com/win/static/stable/x86_64/docker-29.9.1.zip"
  sha256 "5d9b66509390553fd97d590ec2ff0ce10b04c2ac653fa52f2abf2f05c4c197ca"
  license "Apache-2.0"

  # Socktainer serves the Docker Engine REST API on top of Apple's native container runtime. On
  # Windows the native container platform (the Host Compute Service) already has that API in Docker's
  # official static engine build: socktainer runs dockerd.exe on its own named pipe, so it never
  # collides with Docker Desktop's docker_engine pipe.

  livecheck do
    url "https://download.docker.com/win/static/stable/x86_64/"
    regex(/href=.*?docker[._-]v?(\d+(?:\.\d+)+)\.zip/i)
  end

  def install
    install_tree
    windows_exe "libexec/dockerd.exe", "socktainer", "-H", "npipe:////./pipe/socktainer"
  end

  def caveats
    <<~EOS
      socktainer runs Docker's engine for Windows containers. It needs an elevated terminal and the
      Windows "Containers" feature (plus "Hyper-V" for Hyper-V isolation), which a formula cannot enable.
      To connect Docker tools to it, set:
        DOCKER_HOST=npipe:////./pipe/socktainer
    EOS
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/socktainer --version")
    assert_match "--data-root", shell_output("#{bin}/socktainer --help")
  end
end

class DockerClean < Formula
  desc "Clean Docker containers, images, networks, and volumes"
  homepage "https://github.com/ZZROTDesign/docker-clean"
  url "https://github.com/ZZROTDesign/docker-clean/archive/refs/tags/v2.0.4.tar.gz"
  sha256 "4b636fd7391358b60c05b65ba7e89d27eaf8dd56cc516f3c786b59cadac52740"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/docker-clean-2.0.4"
    sha256 cellar: :any, x86_64_windows: "aae00d9fa590bdfbecbd8a3d31181f75d786c79ce5269484628337d73f5a0739"
  end

  def install
    bin.install "docker-clean"
  end

  test do
    system bin/"docker-clean", "--help"
  end
end

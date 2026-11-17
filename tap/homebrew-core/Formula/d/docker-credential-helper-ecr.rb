class DockerCredentialHelperEcr < Formula
  desc "Docker Credential Helper for Amazon ECR"
  homepage "https://github.com/awslabs/amazon-ecr-credential-helper"
  url "https://github.com/awslabs/amazon-ecr-credential-helper/archive/refs/tags/v0.13.0.tar.gz"
  sha256 "95478189f2a3dc6a89f3f047348f4e64443089c0574f6ec0bd864ef44f098bf4"
  license "Apache-2.0"
  head "https://github.com/awslabs/amazon-ecr-credential-helper.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3ea52e065265ab16408fe6ebfdae05e7f09ce78fe41dbd5be25e143d4048ddf8"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download", "-C", "ecr-login"
  end

  def install
    (buildpath/"GITCOMMIT_SHA").write tap.user
    system "make", "build"
    bin.install "bin/local/docker-credential-ecr-login"
  end

  test do
    output = shell_output("#{bin}/docker-credential-ecr-login", 1)
    assert_match(/^Usage: .*docker-credential-ecr-login/, output)
  end
end

class AmazonEcsCli < Formula
  desc "CLI for Amazon ECS to manage clusters and tasks for development"
  homepage "https://aws.amazon.com/ecs/"
  url "https://github.com/aws/amazon-ecs-cli/archive/refs/tags/v1.21.0.tar.gz"
  sha256 "27e93a5439090486a2f2f5a9b02cbbd1493e3c14affbbe2375ed57f8f903e677"
  license "Apache-2.0"
  head "https://github.com/aws/amazon-ecs-cli.git", branch: "mainline"

  deprecate! date: "2025-12-25", because: :repo_archived
  disable! date: "2026-12-25", because: :repo_archived

  bottle do
    sha256 cellar: :any, x86_64_windows: "e295426244f2317ffe6bc8244f4798e12bc0881f2ff3cf3fe6ae231ee579178f"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    ENV["GOPATH"] = buildpath
    ENV["GO111MODULE"] = "auto"
    (buildpath/"src/github.com/aws/amazon-ecs-cli").install buildpath.children
    cd "src/github.com/aws/amazon-ecs-cli" do
      system "make", "build"
      bin.install "bin/local/ecs-cli"
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ecs-cli -v")
  end
end

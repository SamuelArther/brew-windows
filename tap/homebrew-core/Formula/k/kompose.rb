class Kompose < Formula
  desc "Tool to move from `docker-compose` to Kubernetes"
  homepage "https://kompose.io/"
  url "https://github.com/kubernetes/kompose/archive/refs/tags/v1.38.0.tar.gz"
  sha256 "1a6eb3e9a5084d0ce6d1a81628b314686b7cfa41124bace13eb188865f7640a0"
  license "Apache-2.0"
  head "https://github.com/kubernetes/kompose.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0fecaf58321458c826ad992c19da9927d8baac598bb0bccb0b1bb84c887167b5"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = OS.mac? ? "1" : "0"
    system "go", "build", *std_go_args

    generate_completions_from_executable(bin/"kompose", "completion")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kompose version")
  end
end

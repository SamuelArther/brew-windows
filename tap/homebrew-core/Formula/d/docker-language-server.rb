class DockerLanguageServer < Formula
  desc "Language server for Dockerfiles, Compose files, and Bake files"
  homepage "https://www.docker.com"
  url "https://github.com/docker/docker-language-server/archive/refs/tags/v0.20.1.tar.gz"
  sha256 "df94194ce63f0a1217944c72b941a842101aee7b7dd16018d71818d070d146a8"
  license "Apache-2.0"
  head "https://github.com/docker/docker-language-server.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0b737e78eb6c3790a8e4920763baa4a99236e8f483afe332e1225a75c5e61aab"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = OS.mac? ? "1" : "0"

    ldflags = %W[-X github.com/docker/docker-language-server/internal/pkg/cli/metadata.Version=v#{version}]

    system "go", "build", *std_go_args(ldflags:), "./cmd/docker-language-server"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/docker-language-server --version")

    require "open3"

    json = <<~JSON
      {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "initialize",
        "params": {
          "rootUri": null,
          "capabilities": {}
        }
      }
    JSON

    Open3.popen3("docker-language-server", "start", "--stdio") do |stdin, stdout|
      stdin.write "Content-Length: #{json.size}\r\n\r\n#{json}"
      assert_match(/^Content-Length: \d+/i, stdout.readline)
    end
  end
end

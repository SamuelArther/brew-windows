class Mcptools < Formula
  desc "CLI for interacting with MCP servers using both stdio and HTTP transport"
  homepage "https://github.com/f/mcptools"
  url "https://github.com/f/mcptools/archive/refs/tags/v0.7.1.tar.gz"
  sha256 "048250696cd5456f9617a70d92586690973434f0ad6c9aa4481ef914fb0ef8af"
  license "MIT"
  head "https://github.com/f/mcptools.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e092e9c6106a49de057cbe6a7446dd30ea211632f9a09826c77033f030b78613"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=#{version}"), "./cmd/mcptools"

    generate_completions_from_executable(bin/"mcptools", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mcptools version")

    output = shell_output("#{bin}/mcptools configs ls")
    assert_match "No MCP servers found", output
  end
end

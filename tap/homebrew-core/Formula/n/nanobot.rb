class Nanobot < Formula
  desc "Build MCP Agents"
  homepage "https://www.nanobot.ai/"
  url "https://github.com/obot-platform/nanobot/archive/refs/tags/v0.0.92.tar.gz"
  sha256 "1392bd7ac4e93ff25bb0e42c44416674f408ea698208bd11ae6664d3936b9a97"
  license "Apache-2.0"
  head "https://github.com/obot-platform/nanobot.git", branch: "main"

  deprecate! date: "2026-10-24", because: :repo_archived
  disable! date: "2027-01-24", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5046a1a84ca715d8a6c85a3d316f1b19f5d1b46b70f1ce9281e30797b5544305"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/obot-platform/nanobot/pkg/version.Tag=v#{version}
      -X github.com/obot-platform/nanobot/pkg/version.BaseImage=ghcr.io/nanobot-ai/nanobot:v#{version}
    ]
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"nanobot", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nanobot --version")

    pid = spawn bin/"nanobot", "run"
    sleep 1
    assert_path_exists testpath/"nanobot.db"
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end

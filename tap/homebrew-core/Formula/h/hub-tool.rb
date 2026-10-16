class HubTool < Formula
  desc "Docker Hub experimental CLI tool"
  homepage "https://github.com/docker/hub-tool"
  url "https://github.com/docker/hub-tool/archive/refs/tags/v04.6.tar.gz"
  sha256 "e033e027c4b6dc360abf530a00b3ac0caec5ab17788c015336eb59a5e854e7d1"
  license "Apache-2.0"
  head "https://github.com/docker/hub-tool.git", branch: "main"

  deprecate! date: "2026-07-08", because: :repo_archived
  disable! date: "2026-10-08", because: :repo_archived

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/docker/hub-tool/internal.Version=#{version}
      -X github.com/docker/hub-tool/internal.GitCommit=#{tap.user}
    ]

    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hub-tool version")
    output = shell_output("#{bin}/hub-tool token 2>&1", 1)
    assert_match "You need to be logged in to Docker Hub to use this tool", output
  end
end

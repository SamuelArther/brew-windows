class AtomgitCli < Formula
  desc "Command-line interface for AtomGit"
  homepage "https://atomgit.com/hust-open-atom-club/atomgit-cli"
  url "https://raw.atomgit.com/hust-open-atom-club/atomgit-cli/archive/refs/heads/v0.7.5.tar.gz"
  sha256 "b2cda7652c96fce4e573278d355929d2c617a98e86c3bbb68b38d957502b4815"
  license "MulanPSL-2.0"
  head "https://atomgit.com/hust-open-atom-club/atomgit-cli.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "26fcdeaf621b43f60a1b9c1e80727a02331bf29dfb5075afd166ba34355939d6"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X atomgit.com/hust-open-atom-club/atomgit-cli/internal/version.Version=#{version}
    ]
    system "go", "build", *std_go_args(ldflags:, output: bin/"ag-cli"), "./cmd/ag-cli"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ag-cli version")

    system bin/"ag-cli", "alias", "set", "rv", "repo", "view"
    aliases = shell_output("#{bin}/ag-cli alias list")
    assert_match "rv", aliases
    assert_match "repo view", aliases
  end
end

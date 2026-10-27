class Govc < Formula
  desc "Command-line tool for VMware vSphere"
  homepage "https://github.com/vmware/govmomi/tree/main/govc"
  url "https://github.com/vmware/govmomi/archive/refs/tags/v0.56.2.tar.gz"
  sha256 "0a5b002dbf1e396465a62d90ca75c77c9c170e02886dce3b32c9f7cdfb224972"
  license "Apache-2.0"
  head "https://github.com/vmware/govmomi.git", branch: "main"

  # Upstream appears to use GitHub releases to indicate that a version is
  # released (and some tagged versions don't end up as a release), so it's
  # necessary to check release versions instead of tags.
  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3966b66c6b78e065f585e52ccdb7581a6f504a6ea0606b048422e99a8543852e"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download", "-C", "govc"
  end

  def install
    ldflags = %W[
      -X github.com/vmware/govmomi/cli/flags.BuildVersion=#{version}
      -X github.com/vmware/govmomi/cli/flags.BuildCommit=#{tap.user}
      -X github.com/vmware/govmomi/cli/flags.BuildDate=#{time.iso8601}
    ]
    cd "govc" do
      system "go", "build", *std_go_args(ldflags:)
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/govc version")
    assert_match "GOVC_URL=foo", shell_output("#{bin}/govc env -u=foo")
  end
end

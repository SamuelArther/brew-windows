class Elvish < Formula
  desc "Friendly and expressive shell"
  homepage "https://elv.sh/"
  url "https://github.com/elves/elvish/archive/refs/tags/v0.21.0.tar.gz"
  sha256 "3a4b93c3c99fe2f9847de35d64be24e2d4b9c12d429cd9831b4571993a66bb7a"
  license "BSD-2-Clause"
  head "https://github.com/elves/elvish.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e138ffb4fc1582cd1c46115d824f6c8caf03ce90d0529cd183ffd3513ab7fb90"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X src.elv.sh/pkg/buildinfo.VersionSuffix="
    system "go", "build", *std_go_args(ldflags:), "./cmd/elvish"
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/elvish -version").chomp
    assert_match "hello", shell_output("#{bin}/elvish -c 'echo hello'")
  end
end

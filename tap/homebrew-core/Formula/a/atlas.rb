class Atlas < Formula
  desc "Database toolkit"
  homepage "https://atlasgo.io/"
  # Upstream may not mark patch releases as latest on GitHub; it is fine to ship them.
  # See https://github.com/ariga/atlas/issues/1090#issuecomment-1225258408
  url "https://github.com/ariga/atlas/archive/refs/tags/v1.4.0.tar.gz"
  sha256 "572e85b029682e628475b4b8dfc330e10644810a4d7cba3daaa0cd7d7bdc8f34"
  license "Apache-2.0"
  head "https://github.com/ariga/atlas.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e2ac2a11d5680d6b619ec89063ad15749c43456e5d1583c20221648900546aaf"
  end

  depends_on "go" => :build

  conflicts_with "mongodb-atlas-cli", "nim", because: "both install `atlas` executable"

  deny_network_access!

  def fetch
    system "go", "mod", "download", "-C", "cmd/atlas"
  end

  def install
    ldflags = %W[-X ariga.io/atlas/cmd/atlas/internal/cmdapi.version=v#{version}]
    cd "./cmd/atlas" do
      system "go", "build", *std_go_args(ldflags:)
    end

    generate_completions_from_executable(bin/"atlas", shell_parameter_format: :cobra)
  end

  test do
    assert_match "Error: mysql: query system variables:",
      shell_output("#{bin}/atlas schema inspect -u \"mysql://user:pass@localhost:3306/dbname\" 2>&1", 1)

    assert_match version.to_s, shell_output("#{bin}/atlas version")
  end
end

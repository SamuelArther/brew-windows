class Forcecli < Formula
  desc "Command-line interface to Force.com"
  homepage "https://force-cli.herokuapp.com/"
  url "https://github.com/ForceCLI/force/archive/refs/tags/v1.17.0.tar.gz"
  sha256 "9ad6f8b36b945f211b5a5904b4e538d5c30be216d71008860b09fd00619cb30a"
  license "MIT"
  head "https://github.com/ForceCLI/force.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0483686150547de5a2a97bf6b1ea3202d621851c76cbcc39cf6df653e929635e"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(output: bin/"force")

    generate_completions_from_executable(bin/"force", shell_parameter_format: :cobra)
  end

  test do
    assert_match "ERROR: Please login before running this command.",
                 shell_output("#{bin}/force active 2>&1", 1)
  end
end

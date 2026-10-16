class Microplane < Formula
  desc "CLI tool to make git changes across many repos"
  homepage "https://github.com/Clever/microplane"
  url "https://github.com/Clever/microplane/archive/refs/tags/v0.0.37.tar.gz"
  sha256 "437f65748272de89b8a6990f0f9fca57addf6ff6f8e4de077869976d2ce36154"
  license "Apache-2.0"
  head "https://github.com/Clever/microplane.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "018b5e2bc78900b89f2da31846f09c7d75cf8a384309f90a310405779527734c"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(output: bin/"mp", ldflags: "-X main.version=#{version}")

    generate_completions_from_executable(bin/"mp", shell_parameter_format: :cobra)
  end

  test do
    # mandatory env variable
    ENV["GITHUB_API_TOKEN"] = "test"
    # create repos.txt
    (testpath/"repos.txt").write <<~EOF
      hashicorp/terraform
    EOF
    # create mp/init.json
    system bin/"mp", "init", "-f", testpath/"repos.txt"
    # test command
    output = shell_output("#{bin}/mp plan -b microplaning -m 'microplane fun' -r terraform -- sh echo 'hi' 2>&1")
    assert_match "planning", output
  end
end

class Skate < Formula
  desc "Personal key value store"
  homepage "https://github.com/charmbracelet/skate"
  url "https://github.com/charmbracelet/skate/archive/refs/tags/v1.1.0.tar.gz"
  sha256 "98b60c6d78e89467f37d49aca532ba541201737d8824ad251f4fad274f9556e8"
  license "MIT"
  head "https://github.com/charmbracelet/skate.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "efc035dfaf7fd3483b1b04165f770bc03caa272f4b5c2db53e3d90406feda2c9"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    # fang only reads the version from Go module build info, which source builds lack
    inreplace "main.go", "rootCmd)", "rootCmd, fang.WithVersion(\"#{version}\"))"
    system "go", "build", *std_go_args

    generate_completions_from_executable(bin/"skate", shell_parameter_format: :cobra)
  end

  test do
    system bin/"skate", "set", "foo", "bar"
    assert_equal "bar", shell_output("#{bin}/skate get foo").chomp
    assert_match "foo", shell_output("#{bin}/skate list")

    # test unicode
    system bin/"skate", "set", "猫咪", "喵"
    assert_equal "喵", shell_output("#{bin}/skate get 猫咪").chomp

    assert_match version.to_s, shell_output("#{bin}/skate --version")
  end
end

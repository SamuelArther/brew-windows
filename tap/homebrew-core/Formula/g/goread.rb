class Goread < Formula
  desc "RSS/Atom feeds in the terminal"
  homepage "https://github.com/TypicalAM/goread"
  url "https://github.com/TypicalAM/goread/archive/refs/tags/v1.7.3.tar.gz"
  sha256 "9b08cae05593034711c599b6b17605194a11bbfae769b4e7e0076a01ec197c37"
  license "GPL-3.0-or-later"
  head "https://github.com/TypicalAM/goread.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "002dc1ab75b9d67ab6be96c4a9a110176f209619f268a71b532a4a87b365f797"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
    generate_completions_from_executable(bin/"goread", shell_parameter_format: :cobra)
  end

  test do
    output = shell_output("#{bin}/goread --test_colors")
    assert_match "A table of all the colors", output

    assert_match version.to_s, shell_output("#{bin}/goread --version")
  end
end

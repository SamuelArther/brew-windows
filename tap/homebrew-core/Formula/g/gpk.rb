class Gpk < Formula
  desc "TUI and CLI that unifies every package manager on the system"
  homepage "https://github.com/neur0map/glazepkg"
  url "https://github.com/neur0map/glazepkg/archive/refs/tags/v0.7.0.tar.gz"
  sha256 "2571f6005046d77dee9d5f0f4d2e5981887988d43c3cf6c8d1da5e8aa7d91c48"
  license "GPL-3.0-or-later"
  head "https://github.com/neur0map/glazepkg.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "26b7a355da6f5578a5f9db808c43aa926a9fc268de244f80fbe7caf21eb480e6"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=v#{version}", tags: "noselfupdate"), "./cmd/gpk"
    generate_completions_from_executable(bin/"gpk", "completion")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gpk --version")

    # gpk must enumerate the real Homebrew installation it was just installed into.
    listed = JSON.parse(shell_output("#{bin}/gpk list --json --manager brew --quiet"))
    assert_equal 1, listed["schema"]
    assert listed["data"].any? { |pkg| pkg["name"] == "gpk" }, "gpk did not find itself via brew"

    # gpk must recognise the Homebrew keg that owns its binary rather than self-updating.
    assert_match "brew upgrade gpk", shell_output("#{bin}/gpk update 2>&1", 1)
  end
end

class Dstask < Formula
  desc "Git-powered personal task tracker"
  homepage "https://github.com/naggie/dstask"
  url "https://github.com/naggie/dstask/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "afca526d049874e2609d91c0e5f186d614c684ec13b2fe517e00ec4eeb4f70da"
  license "MIT"
  head "https://github.com/naggie/dstask.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "90b22cbd0c00d6a1771bf3561f14d055a6f83e2d4ddd8686c52163a77961e939"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/naggie/dstask.GIT_COMMIT=#{tap.user}
      -X github.com/naggie/dstask.VERSION=#{version}
      -X github.com/naggie/dstask.BUILD_DATE=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:), "./cmd/dstask"
    system "go", "build", *std_go_args(ldflags:, output: bin/"dstask-import"), "./cmd/dstask-import"

    bash_completion.install "completions/bash.sh" => "dstask"
    fish_completion.install "completions/completions.fish" => "dstask.fish"
    zsh_completion.install "completions/zsh.sh" => "_dstask"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dstask version")

    mkdir ".dstask" do
      system "git", "init"
      system "git", "config", "user.name", "BrewTestBot"
      system "git", "config", "user.email", "BrewTestBot@test.com"
    end

    system bin/"dstask", "add", "Brew the brew"
    system bin/"dstask", "start", "1"
    assert_match "Brew the brew", shell_output("#{bin}/dstask show-active")
    system bin/"dstask", "done", "1"
  end
end

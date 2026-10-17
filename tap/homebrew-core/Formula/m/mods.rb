class Mods < Formula
  desc "AI on the command-line"
  homepage "https://github.com/charmbracelet/mods"
  url "https://github.com/charmbracelet/mods/archive/refs/tags/v1.8.1.tar.gz"
  sha256 "e16268ce55b9c90395116c2c8ce4d820d18d7f0b05430d64dc69686410776231"
  license "MIT"
  head "https://github.com/charmbracelet/mods.git", branch: "main"

  deprecate! date: "2026-07-17", because: :repo_archived
  disable! date: "2027-01-17", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a51e97bb22779d2695406db3a4edcfa91b18214931e0ad496303200b11a05b38"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X main.Version=#{version} -X main.CommitSHA=#{tap.user} -X main.CommitDate=#{time.iso8601}"
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"mods", shell_parameter_format: :cobra)
  end

  test do
    output = pipe_output("#{bin}/mods 2>&1", "Hello, Homebrew!", 1)
    assert_match "ERROR  OpenAI authentication failed", output

    assert_match version.to_s, shell_output("#{bin}/mods --version")
    assert_match "GPT on the command line", shell_output("#{bin}/mods --help")
  end
end

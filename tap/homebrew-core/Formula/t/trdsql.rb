class Trdsql < Formula
  desc "CLI tool that can execute SQL queries on CSV, LTSV, JSON, YAML and TBLN"
  homepage "https://noborus.github.io/trdsql/"
  url "https://github.com/noborus/trdsql/releases/download/v1.2.3/trdsql_v1.2.3_windows_amd64.zip"
  sha256 "03d6aa5a593511e8c0a75eb235a161aa8305f5ab2b1b1ac039ed800cab27d823"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/noborus/trdsql/archive/refs/tags/v1.2.3.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/trdsql.exe", "trdsql"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/trdsql --version 2>&1")
  end
end

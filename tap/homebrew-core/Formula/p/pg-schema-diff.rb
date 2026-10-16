class PgSchemaDiff < Formula
  desc "Diff Postgres schemas and generating SQL migrations"
  homepage "https://github.com/stripe/pg-schema-diff"
  url "https://github.com/stripe/pg-schema-diff/archive/refs/tags/v1.0.9.tar.gz"
  sha256 "70b978ee7256fb8693cbc205f749691e4f403f1ed760d619d623f76bfbb138a6"
  license "MIT"
  head "https://github.com/stripe/pg-schema-diff.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a9c247466be60bddfe8273e0f5b516c7913dcbc5d2981882d5475c29ac9cce6d"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/pg-schema-diff"

    generate_completions_from_executable(bin/"pg-schema-diff", shell_parameter_format: :cobra)
  end

  test do
    pg_port = free_port
    dsn = "postgres://postgres:postgres@127.0.0.1:#{pg_port}/postgres?sslmode=disable"

    output = shell_output("#{bin}/pg-schema-diff plan --from-dsn '#{dsn}' --to-dir #{testpath} 2>&1", 1)
    assert_match "Error: creating temp db factory", output
  end
end

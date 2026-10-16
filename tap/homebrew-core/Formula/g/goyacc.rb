class Goyacc < Formula
  desc "Parser Generator for Go"
  homepage "https://pkg.go.dev/modernc.org/goyacc"
  url "https://gitlab.com/cznic/goyacc/-/archive/v1.0.3/goyacc-v1.0.3.tar.bz2"
  sha256 "a999dd35759dfa35cc2cff70b840db0cd41443ef7aff7b090ac170731e6a6c6c"
  license "BSD-3-Clause"
  head "https://gitlab.com/cznic/goyacc.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "22b8ad589db025836d729a582c947b8449340d9591e0e43947c88be4188acf0d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    (testpath/"test.y").write <<~YACC
      %{
      package main
      %}

      %union { val int }
      %token <val> NUM
      %type <val> expr term factor

      %%
      expr : expr '+' term { $$ = $1 + $3; }
          | term          { $$ = $1; }
          ;
      term : term '*' factor { $$ = $1 * $3; }
          | factor          { $$ = $1; }
          ;
      factor : '(' expr ')'  { $$ = $2; }
            | NUM           { $$ = $1; }
            ;
      %%
    YACC

    system bin/"goyacc", "-o", "test.go", "test.y"
    assert_match "package main", (testpath/"test.go").read
  end
end

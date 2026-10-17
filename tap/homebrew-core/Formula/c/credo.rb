class Credo < Formula
  desc "Static code analysis tool for the Elixir"
  homepage "https://github.com/rrrene/credo"
  url "https://github.com/rrrene/credo/archive/refs/tags/v1.7.19.tar.gz"
  sha256 "a4b5a14d574107e1e17857f699584e332f8553cdf465b8d1b0307c4a63c60af5"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0e3da19655ac8306e6c55e5498b65ca659dc43547f2f44a5e4199a8d157768cc"
  end

  depends_on "elixir"

  def install
    ENV["MIX_ENV"] = "prod"
    ENV["MIX_HOME"] = buildpath/".mix"
    ENV["HEX_HOME"] = buildpath/".hex"

    system "mix", "deps.get", "--only", "prod"
    system "mix", "compile"
    system "mix", "escript.build"

    bin.install "credo"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/credo --version")

    (testpath/"test.ex").write <<~ELIXIR
      defmodule Test do
        @moduledoc """
        Test module for Credo.
        """
        def test_fun do
          [1, 2, 3] |> Enum.map(fn(x) -> x * x end)
        end
      end
    ELIXIR

    assert_match "2 mods/funs, found no issues", shell_output("#{bin}/credo #{testpath}/test.ex")
  end
end

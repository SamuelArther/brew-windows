class Elixir < Formula
  desc "Functional metaprogramming aware language built on Erlang VM"
  homepage "https://elixir-lang.org/"
  version "1.20.4"
  url "https://github.com/elixir-lang/elixir/releases/download/v1.20.4/elixir-otp-29.zip"
  sha256 "7863c546cda13fecc949e562e326042451dacf8fd8698a36783cb71eeb223b46"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  depends_on "erlang"

  # Official precompiled Elixir for OTP 29; its .bat launchers use the erl on PATH (erlang).

  def install
    install_tree
    %w[elixir elixirc iex mix].each do |cmd|
      windows_cmd "libexec/bin/#{cmd}.bat", cmd
    end
  end

  test do
    assert_match(/(compiled with Erlang\/OTP 29)/, shell_output("#{bin}/elixir -v"))
  end
end

class Erlang < Formula
  desc "Programming language for highly scalable real-time systems"
  homepage "https://www.erlang.org/"
  url "https://github.com/erlang/otp/releases/download/OTP-29.1.2/otp_win64_29.1.2.zip"
  sha256 "4efe9772a84c6c1ef75377e9326815584958776c905f2bf687b62509e2d809c0"
  license "Apache-2.0"

  livecheck do
    url :stable
    regex(/^OTP[._-]v?(\d+(?:\.\d+)+)$/i)
    strategy :github_latest
  end

  # Official Erlang/OTP Windows x64 zip (relocatable, no installer). It needs the Microsoft
  # Visual C++ redistributable; upstream ships vc_redist.exe inside, which brew never runs.

  def install
    install_tree
    %w[ct_run dialyzer erl erl_call erlc escript typer werl].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
    end
  end

  test do
    system bin/"erl", "-noshell", "-eval", "crypto:start().", "-s", "init", "stop"
    assert_match version.major.to_s,
                 shell_output("#{bin}/erl -noshell -eval \"io:fwrite(erlang:system_info(otp_release)), halt().\"")
  end
end

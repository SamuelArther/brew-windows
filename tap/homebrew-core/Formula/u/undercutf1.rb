class Undercutf1 < Formula
  desc "F1 Live Timing TUI for all F1 sessions with variable delay to sync to your TV"
  homepage "https://amandhoot.com"
  url "https://github.com/JustAman62/undercut-f1/archive/refs/tags/v4.0.103.tar.gz"
  sha256 "58907341f4027ac6ae9019da9893655cfda41accf1fb46d2fff2d5f6fbb0c799"
  license "GPL-3.0-only"
  head "https://github.com/JustAman62/undercut-f1.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1d821e30a3071a4165c46056b3c84a5377d871b6f911d721c6e8fcbea1f7da86"
  end

  depends_on "dotnet"
  depends_on "ffmpeg"
  depends_on "fontconfig"
  depends_on "mpg123"

  def install
    ENV["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"

    dotnet = Formula["dotnet"]
    args = %W[
      --configuration Release
      --framework net#{dotnet.version.major_minor}
      --output #{libexec}
      --no-self-contained
      --use-current-runtime
      -p:PublishSingleFile=true
      -p:IncludeAllContentForSelfExtract=true
      -p:IncludeNativeLibrariesForSelfExtract=true
      -p:EnableCompressionInSingleFile=false
      -p:DebugType=None
      -p:PublicRelease=true
      -p:PublishTrimmed=false
      -p:PublishAot=false
    ]

    # Version override is not needed if cloning from HEAD
    args << "-p:OverridePackageVersion=#{version}" if build.stable?

    system "dotnet", "publish", "UndercutF1.Console/UndercutF1.Console.csproj", *args

    (bin/"undercutf1").write_env_script libexec/"undercutf1", DOTNET_ROOT: "${DOTNET_ROOT:-#{dotnet.opt_libexec}}"
  end

  test do
    # The sandbox denies FSEvents, so .NET's config file watcher would hang
    ENV["DOTNET_USE_POLLING_FILE_WATCHER"] = "1" if OS.mac?

    assert_match version.to_s, shell_output("#{bin}/undercutf1 --version")

    # Run in its own PTY, as .NET opens `/dev/tty` and gets stopped by SIGTTOU in the background
    require "pty"
    require "io/console"

    output = ""
    PTY.spawn(bin/"undercutf1", "import", "2026") do |r, _w, _pid|
      r.winsize = [80, 43]
      begin
        r.each_line { |line| output += line }
      rescue Errno::EIO
        # GNU/Linux raises EIO when read is done on closed pty end
      end
    end
    assert_match "Received HTTP response headers after", output
  end
end

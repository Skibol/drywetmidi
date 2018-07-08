﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Melanchall.DryWetMidi.Common;

namespace Melanchall.DryWetMidi.Smf.Interaction
{
    /// <summary>
    /// Provides methods for getting single collection of notes and rests by the specified
    /// collection of notes.
    /// </summary>
    public static class GetNotesAndRestsUtilities
    {
        #region Constants

        private static readonly object NoSeparationNoteDescriptor = new object();

        #endregion

        #region Methods

        /// <summary>
        /// Iterates through the specified collection of <see cref="Note"/> returning instances of <see cref="Note"/>
        /// and <see cref="Rest"/> where rests calculated using the specified policy.
        /// </summary>
        /// <param name="notes">Collection of <see cref="Note"/> to iterate over.</param>
        /// <param name="restSeparationPolicy">Policy which determines when rests should be returned.</param>
        /// <returns>Collection of <see cref="ITimedObject"/> where an element either <see cref="Note"/>
        /// or <see cref="Rest"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="notes"/> is null.</exception>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="restSeparationPolicy"/> specified an
        /// invalid value.</exception>
        public static IEnumerable<ILengthedObject> GetNotesAndRests(
            this IEnumerable<Note> notes,
            RestSeparationPolicy restSeparationPolicy)
        {
            ThrowIfArgument.IsNull(nameof(notes), notes);
            ThrowIfArgument.IsInvalidEnumValue(nameof(restSeparationPolicy), restSeparationPolicy);

            switch (restSeparationPolicy)
            {
                case RestSeparationPolicy.NoSeparation:
                    return GetNotesAndRests(notes,
                                            n => NoSeparationNoteDescriptor,
                                            false,
                                            false);
                case RestSeparationPolicy.SeparateByChannel:
                    return GetNotesAndRests(notes,
                                            n => n.Channel,
                                            true,
                                            false);
                case RestSeparationPolicy.SeparateByNoteNumber:
                    return GetNotesAndRests(notes,
                                            n => n.NoteNumber,
                                            false,
                                            true);
                case RestSeparationPolicy.SeparateByChannelAndNoteNumber:
                    return GetNotesAndRests(notes,
                                            n => n.GetNoteId(),
                                            true,
                                            true);
            }

            throw new NotSupportedException($"Rest separation policy {restSeparationPolicy} is not supported.");
        }

        private static IEnumerable<ILengthedObject> GetNotesAndRests<TDescriptor>(
            IEnumerable<Note> notes,
            Func<Note, TDescriptor> noteDescriptorGetter,
            bool setRestChannel,
            bool setRestNoteNumber)
        {
            var lastEndTimes = new Dictionary<TDescriptor, long>();

            foreach (var note in notes.Where(n => n != null).OrderBy(n => n.Time))
            {
                var noteDescriptor = noteDescriptorGetter(note);

                long lastEndTime;
                lastEndTimes.TryGetValue(noteDescriptor, out lastEndTime);

                if (note.Time > lastEndTime)
                    yield return new Rest(lastEndTime,
                                          note.Time - lastEndTime,
                                          setRestChannel ? (FourBitNumber?)note.Channel : null,
                                          setRestNoteNumber ? (SevenBitNumber?)note.NoteNumber : null);

                yield return note.Clone();

                lastEndTimes[noteDescriptor] = Math.Max(lastEndTime, note.Time + note.Length);
            }
        }

        #endregion
    }
}
